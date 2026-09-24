/**
 * Daleel AI Chat — Enterprise Dual-Path Architecture
 * TEXT  → fetch() POST /api/dalily-chat/text → RAG API, streamed back as Server-Sent Events
 * VOICE → WebSocket /ws/dalily-chat → Gemini Live API (AUDIO only)
 */
document.addEventListener('DOMContentLoaded', () => {
    const fab          = document.getElementById('dalily-ai-toggle-btn');
    const chatWindow   = document.getElementById('dalily-ai-chat-window');
    const closeBtn     = document.getElementById('dalily-ai-close-btn');
    const newChatBtn   = document.getElementById('dalily-ai-new-chat-btn');
    const reconnectBtn = document.getElementById('dalily-ai-reconnect');
    const inputField   = document.getElementById('dalily-ai-input');
    const sendBtn      = document.getElementById('dalily-ai-send-btn');
    const micBtn       = document.getElementById('dalily-ai-mic-btn');
    const micPulse     = document.getElementById('dalily-ai-mic-pulse');
    const messagesArea = document.getElementById('dalily-ai-messages');
    const statusDot    = document.getElementById('dalily-status-dot');
    const statusLabel  = document.getElementById('dalily-status-label');
    const statusRing   = document.getElementById('dalily-status-ring');
    const connBanner   = document.getElementById('dalily-connection-banner');

    if (!fab || !chatWindow || !messagesArea) return;

    const sessionStorageKey = 'daleel_chat_session_id';
    const welcomeMarkup = messagesArea.innerHTML;
    const isUuid = value => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value || '');

    let sessionId = null;
    const readStoredSessionId = () => {
        try {
            const value = sessionStorage.getItem(sessionStorageKey);
            return isUuid(value) ? value : null;
        } catch (error) {
            console.warn('[DalilyAI] Session storage is unavailable:', error);
            return null;
        }
    };
    const storeSessionId = value => {
        if (!isUuid(value)) return false;
        sessionId = value;
        try { sessionStorage.setItem(sessionStorageKey, value); }
        catch (error) { console.warn('[DalilyAI] Could not persist the chat session:', error); }
        return true;
    };
    const removeStoredSessionId = () => {
        sessionId = null;
        try { sessionStorage.removeItem(sessionStorageKey); }
        catch (error) { console.warn('[DalilyAI] Could not clear the chat session:', error); }
    };

    sessionId = readStoredSessionId();

    // ── State ──
    let isOpen = false;
    let chatHistory = []; // In-memory context: [{role,text}]
    let isTextLoading = false;

    // Voice state
    let voiceWs = null;
    let isRecording = false;
    let audioCtx = null;
    let mediaStream = null;
    let processor = null;
    let playbackCtx = null;
    let thinkingEl = null;
    let nextAudioPlayTime = 0;
    let currentAiStreamTextNode = null;
    let currentAiStreamRawText = "";
    let vadAnalyser = null;
    let vadData = null;
    let vadSilenceTimer = null;
    let isSpeaking = false;
    let rafId = null;
    let markdownLoadPromise = null;
    let voiceUserTurnText = "";
    let pendingNav = null; // { timer, el }

    // Markdown rendering is only needed after the chat is opened. Loading it lazily
    // removes a third-party request from the critical path of every public page.
    const ensureMarkdown = () => {
        if (window.marked) return Promise.resolve(window.marked);
        if (markdownLoadPromise) return markdownLoadPromise;

        markdownLoadPromise = new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/marked/marked.min.js';
            script.async = true;
            script.onload = () => resolve(window.marked);
            script.onerror = () => reject(new Error('Unable to load Markdown renderer.'));
            document.head.appendChild(script);
        });

        return markdownLoadPromise;
    };

    const gsapOk = () => typeof gsap !== 'undefined';
    let thinkingTween = null;
    let micPulseTl = null;

    // ── Auto-Scroll Utility ──
    const scrollToBottom = () => {
        if (!messagesArea) return;
        requestAnimationFrame(() => {
            if (messagesArea) messagesArea.scrollTop = messagesArea.scrollHeight;
        });
    };

    // ── Page Context Extraction (RAG-lite) ──
    const getPageContext = () => {
        try {
            // Target main content area, fall back to body
            const main = document.querySelector('main') || document.querySelector('[role="main"]') || document.body;
            let raw = main.innerText || '';
            // Strip excessive whitespace/newlines
            raw = raw.replace(/\s+/g, ' ').trim();
            // Cap at 4000 chars to stay well within URL/payload limits
            return raw.substring(0, 4000);
        } catch (e) {
            console.warn('[DalilyAI] Could not extract page context:', e);
            return '';
        }
    };

    // ── Connection State ──
    const setState = (state, detail) => {
        const cfgs = {
            idle:        { dot: 'bg-[#10B981]',       lbl: 'Online',        cls: 'text-[#10B981] dark:text-[#10B981]' },
            connecting:  { dot: 'bg-amber-400',  lbl: 'Connecting...', cls: 'text-amber-600 dark:text-amber-400' },
            listening:   { dot: 'bg-red-500',    lbl: 'Listening...',  cls: 'text-red-500 dark:text-red-400' },
            processing:  { dot: 'bg-[#00B2EC]',       lbl: 'Processing...', cls: 'text-[#00B2EC] dark:text-[#00B2EC]' },
            error:       { dot: 'bg-red-500',    lbl: 'Error',         cls: 'text-red-500 dark:text-red-400' }
        };
        const c = cfgs[state] || cfgs.idle;
        if (statusDot) {
            statusDot.className = `w-1.5 h-1.5 rounded-full transition-colors duration-300 ${c.dot}`;
            if (state === 'connecting' || state === 'listening') statusDot.classList.add('animate-ping');
            else statusDot.classList.remove('animate-ping');
        }
        if (statusLabel) { statusLabel.textContent = c.lbl; statusLabel.className = `transition-colors duration-300 ${c.cls}`; }
        if (connBanner) {
            if (state === 'error' && detail) {
                connBanner.className = 'mb-3 px-3 py-2 rounded-xl text-xs text-center font-medium border bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-700/30 text-red-600 dark:text-red-300';
                connBanner.textContent = detail; connBanner.classList.remove('hidden');
            } else { connBanner.classList.add('hidden'); }
        }
        if (reconnectBtn) reconnectBtn.classList.toggle('hidden', state !== 'error');
    };

    // ── UI Toggle ──
    const toggleChat = () => {
        isOpen = !isOpen;
        if (isOpen) {
            ensureMarkdown().catch(error => console.warn('[DalilyAI]', error.message));
            chatWindow.classList.remove('hidden');
            setTimeout(() => { chatWindow.classList.remove('scale-95', 'opacity-0'); chatWindow.classList.add('scale-100', 'opacity-100'); }, 10);
            if (inputField) inputField.focus();
            if (typeof lucide !== 'undefined') lucide.createIcons();
            setState('idle');
        } else {
            cancelPendingNav();
            chatWindow.classList.remove('scale-100', 'opacity-100');
            chatWindow.classList.add('scale-95', 'opacity-0');
            setTimeout(() => chatWindow.classList.add('hidden'), 300);
            fab.classList.remove('scale-0', 'opacity-0');
        }
        if (isOpen) fab.classList.add('scale-0', 'opacity-0');
        saveState();
    };
    fab.addEventListener('click', toggleChat);
    if (closeBtn) closeBtn.addEventListener('click', toggleChat);

    const startNewChat = () => {
        removeStoredSessionId();
        chatHistory = [];
        saveState();
        removeThinking();
        messagesArea.innerHTML = welcomeMarkup;
        setState('idle');
        if (inputField) inputField.focus();
    };
    if (newChatBtn) newChatBtn.addEventListener('click', startNewChat);

    const restoreSession = async () => {
        if (!sessionId) return;

        const sessionBeingRestored = sessionId;
        setState('connecting');
        try {
            const response = await fetch(`/api/dalily-chat/session/${encodeURIComponent(sessionBeingRestored)}`);
            if (sessionId !== sessionBeingRestored) return;
            if (response.status === 400 || response.status === 404) {
                startNewChat();
                return;
            }
            if (!response.ok) throw new Error(`Server error (${response.status})`);

            const data = await response.json();
            if (!storeSessionId(data.sessionId) || !Array.isArray(data.messages)) {
                throw new Error('The server returned an invalid chat session.');
            }

            const messages = data.messages.filter(message =>
                (message.role === 'user' || message.role === 'assistant') &&
                typeof message.content === 'string' &&
                message.content.trim().length > 0);

            if (messages.length > 0) {
                messagesArea.innerHTML = '';
                chatHistory = [];
                messages.forEach(message => {
                    const isUser = message.role === 'user';
                    appendMessage(isUser ? 'User' : 'AI', message.content);
                    chatHistory.push({ role: isUser ? 'user' : 'model', text: message.content });
                });
            }
            setState('idle');
        } catch (error) {
            console.error('[DalilyAI] Could not restore the chat session:', error);
            setState('error', 'Could not restore the previous chat. You can retry or start a new chat.');
        }
    };
    if (reconnectBtn) reconnectBtn.addEventListener('click', restoreSession);

    // ═══════════════════════════════════════════════
    //  TEXT FLOW — fetch() POST /api/dalily-chat/text (streamed)
    // ═══════════════════════════════════════════════
    // EventSource only does GET, so the POST response body is read and split into
    // "event: name / data: json" blocks by hand.
    const readEventStream = async (response, onEvent) => {
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        const flushBlocks = () => {
            let boundary;
            while ((boundary = buffer.indexOf('\n\n')) !== -1) {
                const block = buffer.slice(0, boundary);
                buffer = buffer.slice(boundary + 2);
                let event = 'message';
                let data = '';
                block.split('\n').forEach(line => {
                    if (line.startsWith('event:')) event = line.slice(6).trim();
                    else if (line.startsWith('data:')) data += line.slice(5).trim();
                });
                if (data) onEvent(event, JSON.parse(data));
            }
        };

        for (;;) {
            const { value, done } = await reader.read();
            if (done) break;
            buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n');
            flushBlocks();
        }
        buffer += '\n\n';
        flushBlocks();
    };

    const handleSend = async () => {
        const text = (inputField?.value || '').trim();
        if (!text || isTextLoading) return;

        cancelPendingNav();
        appendMessage('User', text);
        chatHistory.push({ role: 'user', text });
        saveState();
        inputField.value = '';
        isTextLoading = true;
        setState('processing');
        showThinking();

        try {
            const resp = await fetch('/api/dalily-chat/text', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'text/event-stream' },
                body: JSON.stringify({ history: chatHistory, pageContext: getPageContext(), sessionId })
            });

            if (!resp.ok) {
                removeThinking();
                const err = await resp.json().catch(() => ({}));
                console.error('[DalilyAI] Text API error:', resp.status, err);
                sysMsg(err.error || `Server error (${resp.status})`, 'error');
                setState('error', err.error);
                isTextLoading = false;
                return;
            }

            let aiNode = null;
            let aiText = '';
            let finished = false;
            let failure = null;
            let navigate = null;

            const renderAnswer = () => {
                if (!aiNode) {
                    removeThinking();
                    aiNode = appendMessage('AI', '');
                }
                aiNode.innerHTML = typeof marked !== 'undefined' ? marked.parse(aiText) : esc(aiText);
                scrollToBottom();
            };

            await readEventStream(resp, (event, data) => {
                if (event === 'session') {
                    // arrives before the answer, so the conversation survives a failed stream
                    storeSessionId(data.sessionId);
                } else if (event === 'token') {
                    aiText += data.text || '';
                    renderAnswer();
                } else if (event === 'done') {
                    finished = true;
                    navigate = data.navigate || null;
                    // the stored answer drops a last sentence the length limit cut off mid-stream
                    if (data.answer && data.answer !== aiText) {
                        aiText = data.answer;
                        renderAnswer();
                    }
                    // A navigation-only fallback (RAG unavailable) may carry no session yet.
                    if (data.sessionId != null && !storeSessionId(data.sessionId))
                        console.error('[DalilyAI] Text API returned an invalid session ID:', data.sessionId);
                } else if (event === 'error') {
                    failure = data.error || 'The assistant could not answer.';
                    navigate = data.navigate || null;
                }
            });
            removeThinking();

            if (finished && aiText) {
                chatHistory.push({ role: 'model', text: aiText });
                saveState();
                setState('idle');
            } else {
                failure = failure || 'The answer was interrupted.';
                console.error('[DalilyAI] Text stream failed:', failure);
                sysMsg(failure, 'error');
                setState('error', failure);
            }
            if (navigate) scheduleNavigation(navigate);
        } catch (e) {
            removeThinking();
            console.error('[DalilyAI] Network error:', e);
            sysMsg('Network error. Check your connection.', 'error');
            setState('error', 'Network error');
        }
        isTextLoading = false;
    };

    if (sendBtn) sendBtn.addEventListener('click', handleSend);
    if (inputField) inputField.addEventListener('keypress', e => { if (e.key === 'Enter') handleSend(); });

    // ═══════════════════════════════════════════════
    //  VOICE FLOW — WebSocket /ws/dalily-chat (AUDIO only)
    // ═══════════════════════════════════════════════
    if (micBtn) micBtn.addEventListener('click', async () => {
        // IMPORTANT: Initialize AudioContexts synchronously within the user gesture
        try {
            if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            if (audioCtx.state === 'suspended') audioCtx.resume();
            
            if (!playbackCtx) playbackCtx = new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 24000 });
            if (playbackCtx.state === 'suspended') playbackCtx.resume();
        } catch (e) {
            console.warn('[DalilyAI] AudioContext sync init warning:', e);
        }

        if (!isRecording) await startVoice();
        else stopVoice();
    });

    const startVoice = async () => {
        try {
            console.log('[DalilyAI] Requesting microphone access...');
            mediaStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    channelCount: 1,
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true
                }
            });
            const track = mediaStream.getAudioTracks()[0];
            const settings = track.getSettings();
            console.log(`[DalilyAI] ✅ Mic granted. Track: ${track.label}, Rate: ${settings.sampleRate || 'unknown'}Hz, Channels: ${settings.channelCount || 'unknown'}`);
        } catch (err) {
            console.error('[DalilyAI] ❌ getUserMedia FAILED.');
            console.error('[DalilyAI]   name:', err.name);
            console.error('[DalilyAI]   message:', err.message);
            console.error('[DalilyAI]   code:', err.code);
            console.error('[DalilyAI]   stack:', err.stack);
            console.error('[DalilyAI]   constraint:', err.constraint || 'none');
            const msgs = {
                NotFoundError: 'No microphone found on this device.',
                NotAllowedError: 'Microphone permission denied. Please allow access in browser settings.',
                NotReadableError: 'Microphone is in use by another application.',
                OverconstrainedError: `Browser rejected audio constraints: ${err.constraint || 'unknown'}.`,
                AbortError: 'Microphone request was aborted.',
                SecurityError: 'Microphone access blocked by security policy (requires HTTPS).'
            };
            sysMsg(msgs[err.name] || `Microphone error: ${err.name} — ${err.message}`, 'error');
            return;
        }

        isRecording = true;
        setState('connecting');
        micBtn.classList.add('text-red-500', 'bg-red-100', 'dark:bg-red-900/30');
        micBtn.classList.remove('text-slate-500', 'dark:text-slate-400', 'bg-slate-100', 'dark:bg-slate-800');

        if (gsapOk() && micPulse && micPulse.parentNode) gsap.set(micPulse, { opacity: 0, scale: 1 });

        if (!voiceWs || voiceWs.readyState !== WebSocket.OPEN) {
            const proto = location.protocol === 'https:' ? 'wss:' : 'ws:';
            voiceWs = new WebSocket(`${proto}//${location.host}/ws/dalily-chat`);
            voiceWs.onopen = () => {
                console.log('[DalilyAI] Voice WS connected, sending init_context...');
                voiceWs.send(JSON.stringify({ type: 'init_context', context: getPageContext() }));
            };
            voiceWs.onmessage = e => { try { handleVoiceMsg(JSON.parse(e.data)); } catch (err) { console.error('[DalilyAI] Parse error:', err); } };
            voiceWs.onerror = err => { console.error('[DalilyAI] Voice WS error:', err); };
            voiceWs.onclose = ev => {
                console.warn(`[DalilyAI] Voice WS closed — code:${ev.code} reason:${ev.reason}`);
                if (isRecording) stopVoice();
            };

            try {
                await new Promise((res, rej) => {
                    const origMsg = voiceWs.onmessage;
                    voiceWs.onmessage = e => {
                        try {
                            const m = JSON.parse(e.data);
                            if (m.type === 'voiceReady') { voiceWs.onmessage = origMsg; res(); }
                            else if (m.type === 'error') { rej(new Error(m.text)); }
                            else if (origMsg) origMsg(e);
                        } catch (err) { rej(err); }
                    };
                    voiceWs.onerror = () => rej(new Error('WS error'));
                    setTimeout(() => rej(new Error('Voice setup timeout')), 15000);
                });
            } catch (err) {
                console.error('[DalilyAI] Voice setup failed:', err);
                sysMsg(err.message || 'Voice setup failed.', 'error');
                stopVoice();
                return;
            }
        }

        if (!mediaStream || !mediaStream.active) {
            console.error('[DalilyAI] MediaStream is null/inactive after WS setup. Aborting audio capture.');
            sysMsg('Microphone stream lost during connection. Please try again.', 'error');
            stopVoice();
            return;
        }

        setState('listening');
        sysMsg('Listening... Click mic again to stop.', 'info');

        try {
            if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            console.log(`[DalilyAI] AudioContext active. Native sampleRate: ${audioCtx.sampleRate}Hz, state: ${audioCtx.state}`);

            if (audioCtx.state === 'suspended') {
                console.log('[DalilyAI] AudioContext suspended — calling resume()...');
                await audioCtx.resume();
                console.log(`[DalilyAI] AudioContext resumed. State: ${audioCtx.state}`);
            }

            const nativeRate = audioCtx.sampleRate;
            const targetRate = 16000;
            const downsampleRatio = Math.round(nativeRate / targetRate);

            console.log(`[DalilyAI] Resample ratio: ${downsampleRatio} (${nativeRate} → ${targetRate}Hz)`);

            const src = audioCtx.createMediaStreamSource(mediaStream);
            processor = audioCtx.createScriptProcessor(2048, 1, 1);

            vadAnalyser = audioCtx.createAnalyser();
            vadAnalyser.fftSize = 256;
            vadData = new Uint8Array(vadAnalyser.frequencyBinCount);
            src.connect(vadAnalyser);

            const checkVolume = () => {
                if (!isRecording) return;
                vadAnalyser.getByteFrequencyData(vadData);
                let sum = 0;
                for (let i = 0; i < vadData.length; i++) sum += vadData[i];
                let avg = sum / vadData.length;

                if (gsapOk() && micPulse && micPulse.parentNode) {
                    const scale = 1 + (avg / 255) * 1.5;
                    gsap.to(micPulse, { scale: scale, opacity: avg > 5 ? Math.min(0.8, avg / 50) : 0, duration: 0.1 });
                } else if (micPulse) {
                    const norm = Math.min(avg / 80, 1);
                    const scale = 1 + norm * 0.6;
                    micPulse.style.transform = `scale(${scale.toFixed(2)})`;
                    micPulse.style.opacity = (0.4 + norm * 0.6).toFixed(2);
                    micPulse.classList.remove('hidden');
                }

                if (avg > 5) {
                    if (!isSpeaking) {
                        isSpeaking = true;
                        setState('listening');
                        removeThinking();
                    }
                    if (vadSilenceTimer) { clearTimeout(vadSilenceTimer); vadSilenceTimer = null; }
                } else {
                    if (isSpeaking && !vadSilenceTimer) {
                        vadSilenceTimer = setTimeout(() => {
                            isSpeaking = false;
                            setState('processing');
                            showThinking();
                        }, 1000);
                    }
                }
                rafId = requestAnimationFrame(checkVolume);
            };
            checkVolume();

            processor.onaudioprocess = e => {
                if (!isRecording || !voiceWs || voiceWs.readyState !== WebSocket.OPEN) return;

                const inputData = e.inputBuffer.getChannelData(0);
                const outputLength = Math.floor(inputData.length / downsampleRatio);
                const i16 = new Int16Array(outputLength);
                for (let i = 0; i < outputLength; i++) {
                    const s = Math.max(-1, Math.min(1, inputData[i * downsampleRatio]));
                    i16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
                }

                const bytes = new Uint8Array(i16.buffer);
                let bin = '';
                for (let j = 0; j < bytes.length; j++) bin += String.fromCharCode(bytes[j]);
                voiceWs.send(JSON.stringify({ type: 'audio', data: btoa(bin) }));
            };

            src.connect(processor);
            processor.connect(audioCtx.destination);
            console.log('[DalilyAI] ✅ Audio pipeline active. Streaming 16kHz PCM to Gemini.');
        } catch (err) {
            console.error('[DalilyAI] ❌ Audio capture initialization FAILED.');
            console.error('[DalilyAI]   Error name:', err.name);
            console.error('[DalilyAI]   Error message:', err.message);
            console.error('[DalilyAI]   Error code:', err.code);
            console.error('[DalilyAI]   Stack:', err.stack);
            console.error('[DalilyAI]   AudioContext state:', audioCtx ? audioCtx.state : 'null');
            console.error('[DalilyAI]   MediaStream active:', mediaStream ? mediaStream.active : 'null');
            sysMsg(`Audio capture failed: ${err.name} — ${err.message}`, 'error');
            stopVoice();
        }
    };

    const stopVoice = () => {
        isRecording = false;
        if (processor) { processor.disconnect(); processor = null; }
        if (audioCtx) { audioCtx.close().catch(() => {}); audioCtx = null; }
        if (mediaStream) { mediaStream.getTracks().forEach(t => t.stop()); mediaStream = null; }

        micBtn?.classList.remove('text-red-500', 'bg-red-100', 'dark:bg-red-900/30');
        micBtn?.classList.add('text-slate-500', 'dark:text-slate-400', 'bg-slate-100', 'dark:bg-slate-800');
        if (micPulseTl) { micPulseTl.kill(); micPulseTl = null; }
        if (rafId) { cancelAnimationFrame(rafId); rafId = null; }
        if (vadSilenceTimer) { clearTimeout(vadSilenceTimer); vadSilenceTimer = null; }
        isSpeaking = false;
        if (gsapOk() && micPulse && micPulse.parentNode) gsap.set(micPulse, { opacity: 0, scale: 1 });
        else if (micPulse) { micPulse.style.transform = ''; micPulse.style.opacity = ''; micPulse.classList.add('hidden'); }

        setState('idle');
    };

    const handleVoiceMsg = msg => {
        switch (msg.type) {
            case 'audio': 
                removeThinking();
                setState('idle');
                playAudio(msg.data, msg.mimeType); 
                break;
            case 'voiceTranscript':
                if (msg.sender === 'user') {
                    appendMessage('User', msg.text);
                    voiceUserTurnText += (voiceUserTurnText ? ' ' : '') + msg.text;
                } else {
                    removeThinking();
                    setState('idle');
                    streamAIText(msg.text);
                }
                break;
            case 'turnComplete': 
                removeThinking();
                if (voiceUserTurnText) chatHistory.push({ role: 'user', text: voiceUserTurnText });
                if (currentAiStreamRawText) chatHistory.push({ role: 'model', text: currentAiStreamRawText });
                saveState();
                routeVoiceTurn(voiceUserTurnText);
                voiceUserTurnText = "";
                currentAiStreamTextNode = null;
                currentAiStreamRawText = "";
                break;
            case 'error': sysMsg(msg.text || 'Voice error.', 'error'); break;
            case 'voiceReady': break;
            default: console.log('[DaleelAI] Unknown voice msg:', msg);
        }
    };

    // ── Audio Playback ──
    const playAudio = (b64, mime) => {
        try {
            if (!playbackCtx) {
                playbackCtx = new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 24000 });
                nextAudioPlayTime = playbackCtx.currentTime;
            }
            const bin = atob(b64); const bytes = new Uint8Array(bin.length);
            for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
            const i16 = new Int16Array(bytes.buffer); const f32 = new Float32Array(i16.length);
            for (let i = 0; i < i16.length; i++) f32[i] = i16[i] / 32768.0;
            const buf = playbackCtx.createBuffer(1, f32.length, 24000);
            buf.getChannelData(0).set(f32);
            const s = playbackCtx.createBufferSource(); s.buffer = buf; s.connect(playbackCtx.destination); 
            
            if (nextAudioPlayTime < playbackCtx.currentTime) {
                nextAudioPlayTime = playbackCtx.currentTime + 0.05;
            }
            s.start(nextAudioPlayTime);
            nextAudioPlayTime += buf.duration;
        } catch (e) { console.error('[DaleelAI] Playback error:', e); }
    };

    const streamAIText = (text) => {
        if (!currentAiStreamTextNode) {
            currentAiStreamRawText = "";
            const div = document.createElement('div');
            div.className = 'flex gap-3 mt-4';
            div.innerHTML = `<div class="w-8 h-8 rounded-full bg-[#00B2EC]/10 flex-shrink-0 flex items-center justify-center text-[#00B2EC] mt-1"><span class="material-symbols-outlined text-[16px]">smart_toy</span></div><div class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-white/5 rounded-2xl rounded-tl-sm p-3 text-slate-800 dark:text-slate-200 shadow-sm w-full overflow-x-hidden ai-stream-text prose prose-sm dark:prose-invert prose-p:leading-relaxed prose-pre:bg-slate-900 prose-pre:text-slate-100 max-w-[85%]"></div>`;
            messagesArea.appendChild(div);
            currentAiStreamTextNode = div.querySelector('.ai-stream-text');
        }
        currentAiStreamRawText += text;
        currentAiStreamTextNode.innerHTML = typeof marked !== 'undefined' ? marked.parse(currentAiStreamRawText) : esc(currentAiStreamRawText);
        scrollToBottom();
    };

    // ── Message Rendering ──
    const appendMessage = (sender, text) => {
        const div = document.createElement('div');
        div.className = 'flex gap-3 mt-4';
        if (sender === 'User') {
            div.classList.add('flex-row-reverse');
            div.innerHTML = `<div class="bg-[#00B2EC] rounded-2xl rounded-tr-sm p-3 text-sm text-white shadow-sm leading-relaxed max-w-[85%] break-words">${esc(text)}</div>`;
        } else {
            const parsedText = typeof marked !== 'undefined' ? marked.parse(text) : esc(text);
            div.innerHTML = `<div class="w-8 h-8 rounded-full bg-brand-navy/10 dark:bg-white/10 flex-shrink-0 flex items-center justify-center text-brand-navy dark:text-white mt-1"><span class="material-symbols-outlined text-[16px]">smart_toy</span></div><div class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-white/5 rounded-2xl rounded-tl-sm p-3 text-slate-800 dark:text-slate-200 shadow-sm w-full overflow-x-hidden prose prose-sm dark:prose-invert prose-p:leading-relaxed prose-pre:bg-slate-900 prose-pre:text-slate-100 max-w-[85%]">${parsedText}</div>`;
        }
        messagesArea.appendChild(div);
        scrollToBottom();
        return div.lastElementChild;
    };

    const sysMsg = (text, sev = 'info') => {
        const colors = { info: 'bg-slate-100 dark:bg-slate-800/60 text-slate-500 dark:text-slate-400 border-slate-200 dark:border-slate-700/40', warn: 'bg-amber-50 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-700/30', error: 'bg-red-50 dark:bg-red-900/20 text-red-500 dark:text-red-400 border-red-200 dark:border-red-700/30' };
        const icons = { info: 'info', warn: 'warning', error: 'error' };
        const div = document.createElement('div'); div.className = 'flex justify-center mt-3';
        div.innerHTML = `<div class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium border ${colors[sev] || colors.info}"><span class="material-symbols-outlined text-[16px]">${icons[sev] || 'info'}</span><span>${esc(text)}</span></div>`;
        messagesArea.appendChild(div); scrollToBottom();
    };

    // ── Thinking Indicator ──
    const showThinking = () => {
        if (thinkingEl) return;
        thinkingEl = document.createElement('div'); thinkingEl.className = 'flex gap-3 mt-4';
        thinkingEl.innerHTML = `<div class="w-8 h-8 rounded-full bg-brand-navy/10 dark:bg-white/10 flex-shrink-0 flex items-center justify-center text-brand-navy dark:text-white mt-1"><span class="material-symbols-outlined text-[16px]">smart_toy</span></div><div class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-white/5 rounded-2xl rounded-tl-sm px-4 py-3 shadow-sm flex items-center gap-1.5"><span class="animate-bounce inline-block">•</span><span class="animate-bounce inline-block" style="animation-delay:150ms">•</span><span class="animate-bounce inline-block" style="animation-delay:300ms">•</span></div>`;
        messagesArea.appendChild(thinkingEl); scrollToBottom();
    };
    const removeThinking = () => {
        if (thinkingTween) { thinkingTween.kill(); thinkingTween = null; }
        if (thinkingEl) { thinkingEl.remove(); thinkingEl = null; }
    };

    const esc = s => { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; };

    // ═══════════════════════════════════════════════
    //  PAGE NAVIGATION — open the page that answers the question
    // ═══════════════════════════════════════════════
    const STATE_KEY = 'dalily-chat-state';
    const NAV_DELAY_MS = 3000;
    const isRtl = document.documentElement.dir === 'rtl';
    const t = (ar, en) => (isRtl ? ar : en);

    // Keeps the conversation (and whether the chat is open) across the page change.
    const saveState = (overrides) => {
        try {
            const prev = JSON.parse(sessionStorage.getItem(STATE_KEY) || '{}');
            sessionStorage.setItem(STATE_KEY, JSON.stringify(Object.assign({
                open: isOpen,
                history: chatHistory.slice(-40),
                arrivedAt: prev.arrivedAt || null
            }, overrides)));
        } catch (e) { /* storage unavailable: chat still works, it just won't survive a page change */ }
    };

    // "/Home/About", "/about/" and "/home/about" are the same page.
    const normalizePath = p => {
        const path = (p || '/').toLowerCase().split(/[?#]/)[0].replace(/\/+$/, '').replace(/^\/home(?=\/|$)/, '');
        return path === '' || path === '/index' ? '/' : path;
    };

    const cancelPendingNav = () => {
        if (!pendingNav) return;
        clearTimeout(pendingNav.timer);
        pendingNav.el.remove();
        pendingNav = null;
    };

    const navigateNow = (target) => {
        cancelPendingNav();
        if (isRecording) stopVoice();
        saveState({ open: true, arrivedAt: target });
        window.location.href = target.url;
    };

    const scheduleNavigation = (target, delayMs = NAV_DELAY_MS) => {
        if (!target || !target.url) return;
        const title = t(target.titleAr, target.titleEn);
        cancelPendingNav();

        if (normalizePath(target.url) === normalizePath(location.pathname)) {
            sysMsg(t(`أنت بالفعل في صفحة ${title}`, `You're already on the ${title} page`), 'info');
            return;
        }

        const el = document.createElement('div');
        el.className = 'dalily-nav-card';
        el.innerHTML = `
            <div class="dalily-nav-card__row">
                <span class="material-symbols-outlined dalily-nav-card__icon">near_me</span>
                <div class="dalily-nav-card__text">
                    <span class="dalily-nav-card__label">${esc(t('جاري نقلك إلى صفحة', 'Taking you to'))}</span>
                    <strong>${esc(title)}</strong>
                </div>
                <button type="button" class="dalily-nav-card__go">${esc(t('انتقل الآن', 'Go now'))}</button>
                <button type="button" class="dalily-nav-card__cancel" aria-label="${esc(t('إلغاء', 'Cancel'))}" title="${esc(t('إلغاء', 'Cancel'))}">
                    <span class="material-symbols-outlined">close</span>
                </button>
            </div>
            <div class="dalily-nav-card__bar"><span style="animation-duration:${delayMs}ms"></span></div>`;
        messagesArea.appendChild(el);
        scrollToBottom();

        el.querySelector('.dalily-nav-card__go').addEventListener('click', () => navigateNow(target));
        el.querySelector('.dalily-nav-card__cancel').addEventListener('click', () => {
            cancelPendingNav();
            sysMsg(t('تم إلغاء الانتقال', 'Navigation cancelled'), 'info');
        });

        pendingNav = { el, timer: setTimeout(() => navigateNow(target), delayMs) };
    };

    // Voice: ask the server which page the spoken question belongs to, then let the
    // spoken answer finish playing before leaving the page.
    const routeVoiceTurn = async (spoken) => {
        if (!spoken || !spoken.trim()) return;
        try {
            const resp = await fetch('/api/dalily-chat/route', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ text: spoken })
            });
            if (!resp.ok) return;
            const data = await resp.json();
            if (!data.navigate) return;
            const remainingAudioMs = playbackCtx
                ? Math.max(0, (nextAudioPlayTime - playbackCtx.currentTime) * 1000)
                : 0;
            scheduleNavigation(data.navigate, Math.max(NAV_DELAY_MS, remainingAudioMs + 800));
        } catch (e) {
            console.warn('[DalilyAI] Page routing failed:', e);
        }
    };

    // Restore the conversation after the assistant moved the visitor to another page.
    const restoreState = async () => {
        let state = null;
        try { state = JSON.parse(sessionStorage.getItem(STATE_KEY) || 'null'); } catch (e) { state = null; }
        if (!state) return;

        // A server session is the source of truth for history; restoreSession() renders it.
        if (!sessionId && Array.isArray(state.history) && state.history.length) {
            await ensureMarkdown().catch(() => {});
            chatHistory = state.history;
            chatHistory.forEach(m => appendMessage(m.role === 'user' ? 'User' : 'AI', m.text));
        }

        if (state.arrivedAt) {
            sysMsg(t(`أنت الآن في صفحة ${state.arrivedAt.titleAr}`, `You're now on the ${state.arrivedAt.titleEn} page`), 'info');
            saveState({ open: !!state.open, arrivedAt: null });
        }

        if (state.open && !isOpen) toggleChat();
    };

    restoreState().then(restoreSession);
});

