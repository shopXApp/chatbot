(function() {
    'use strict';
    
    // Configuration and State Management
    class ChatWidget {
        constructor() {
            this.config = this.extractConfig();
            this.state = {
                isOpen: false,
                isMinimized: true,
                currentView: 'greeting', // greeting, form, chat
                sessionId: null,
                chatHistory: this.loadChatHistory(),
                userInfo: null,
                isTyping: false,
                isOnline: true
            };
            this.knowledgeBase = this.initializeKnowledgeBase();
            this.init();
        }

        extractConfig() {
            const scripts = document.getElementsByTagName('script');
            let config = {
                key: 'demo_key',
                secret: 'demo_secret',
                greeting: 'Hello! How can we help you today?',
                position: 'bottom-right',
                theme: 'green',
                companyName: 'XLNC Technologies'
            };

            for (let script of scripts) {
                if (script.src && script.src.includes('tawkWidget.js')) {
                    config.key = script.getAttribute('key') || config.key;
                    config.secret = script.getAttribute('secret') || config.secret;
                    config.greeting = script.getAttribute('greeting') || config.greeting;
                    config.position = script.getAttribute('position') || config.position;
                    config.theme = script.getAttribute('theme') || config.theme;
                    config.companyName = script.getAttribute('company') || config.companyName;
                    break;
                }
            }
            return config;
        }

        initializeKnowledgeBase() {
            return {
                'services': 'We offer web development, mobile app development, cloud solutions, AI integration, and digital transformation services.',
                'pricing': 'Our pricing varies based on project requirements. Please schedule a consultation for a detailed quote.',
                'support': 'We provide 24/7 technical support for all our enterprise clients.',
                'contact': 'You can reach us at info@xlnc.com or call +1-555-0123.',
                'hours': 'Our business hours are Monday to Friday, 9 AM to 6 PM EST.',
                'technologies': 'We work with React, Angular, Node.js, .NET Core, Python, AWS, Azure, and many other modern technologies.'
            };
        }

        init() {
            this.createStyles();
            this.createWidget();
            this.bindEvents();
            this.handleResponsive();
        }

        createStyles() {
            const styles = `
                .xlnc-chat-widget {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    position: fixed;
                    z-index: 999999;
                    ${this.getPositionStyles()}
                }

                .xlnc-chat-widget * {
                    box-sizing: border-box;
                }

                .xlnc-chat-bubble {
                    width: 60px;
                    height: 60px;
                    background: #00d084;
                    border-radius: 50%;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    box-shadow: 0 4px 16px rgba(0, 208, 132, 0.3);
                    transition: all 0.3s ease;
                    position: relative;
                }

                .xlnc-chat-bubble:hover {
                    transform: scale(1.1);
                    box-shadow: 0 6px 20px rgba(0, 208, 132, 0.4);
                }

                .xlnc-chat-bubble svg {
                    width: 24px;
                    height: 24px;
                    fill: white;
                }

                .xlnc-chat-notification {
                    position: absolute;
                    top: -5px;
                    right: -5px;
                    background: #ff4757;
                    color: white;
                    border-radius: 50%;
                    width: 20px;
                    height: 20px;
                    font-size: 12px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-weight: bold;
                }

                .xlnc-chat-window {
                    width: 360px;
                    height: 500px;
                    background: white;
                    border-radius: 12px;
                    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12);
                    display: none;
                    flex-direction: column;
                    overflow: hidden;
                    margin-bottom: 10px;
                }

                .xlnc-chat-window.open {
                    display: flex;
                    animation: slideUp 0.3s ease-out;
                }

                @keyframes slideUp {
                    from {
                        opacity: 0;
                        transform: translateY(20px);
                    }
                    to {
                        opacity: 1;
                        transform: translateY(0);
                    }
                }

                .xlnc-chat-header {
                    background: #00d084;
                    color: white;
                    padding: 16px;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }

                .xlnc-chat-header h3 {
                    margin: 0;
                    font-size: 16px;
                    font-weight: 600;
                }

                .xlnc-chat-controls {
                    display: flex;
                    gap: 8px;
                    align-items: center;
                }

                .xlnc-chat-menu-btn, .xlnc-chat-close-btn {
                    background: none;
                    border: none;
                    color: white;
                    cursor: pointer;
                    padding: 4px;
                    border-radius: 4px;
                    transition: background 0.2s;
                }

                .xlnc-chat-menu-btn:hover, .xlnc-chat-close-btn:hover {
                    background: rgba(255, 255, 255, 0.1);
                }

                .xlnc-chat-body {
                    flex: 1;
                    display: flex;
                    flex-direction: column;
                    position: relative;
                }

                .xlnc-greeting-view {
                    padding: 20px;
                    text-align: center;
                }

                .xlnc-greeting-text {
                    margin: 0 0 16px 0;
                    color: #333;
                    line-height: 1.5;
                    font-size: 14px;
                }

                .xlnc-start-chat-btn {
                    background: #00d084;
                    color: white;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 6px;
                    cursor: pointer;
                    font-weight: 600;
                    transition: background 0.2s;
                    width: 100%;
                }

                .xlnc-start-chat-btn:hover {
                    background: #00b874;
                }

                .xlnc-form-view {
                    padding: 20px;
                }

                .xlnc-form-field {
                    margin-bottom: 16px;
                }

                .xlnc-form-field label {
                    display: block;
                    margin-bottom: 6px;
                    font-weight: 500;
                    color: #333;
                    font-size: 14px;
                }

                .xlnc-form-field input, .xlnc-form-field select {
                    width: 100%;
                    padding: 10px;
                    border: 2px solid #e1e5e9;
                    border-radius: 6px;
                    font-size: 14px;
                    transition: border-color 0.2s;
                }

                .xlnc-form-field input:focus, .xlnc-form-field select:focus {
                    outline: none;
                    border-color: #00d084;
                }

                .xlnc-form-field.error input {
                    border-color: #ff4757;
                }

                .xlnc-form-error {
                    color: #ff4757;
                    font-size: 12px;
                    margin-top: 4px;
                }

                .xlnc-phone-container {
                    display: flex;
                    gap: 8px;
                }

                .xlnc-country-code {
                    width: 80px;
                }

                .xlnc-submit-btn {
                    background: #00d084;
                    color: white;
                    border: none;
                    padding: 12px;
                    border-radius: 6px;
                    cursor: pointer;
                    font-weight: 600;
                    width: 100%;
                    transition: background 0.2s;
                }

                .xlnc-submit-btn:hover {
                    background: #00b874;
                }

                .xlnc-chat-view {
                    height: 100%;
                    display: flex;
                    flex-direction: column;
                }

                .xlnc-chat-messages {
                    flex: 1;
                    padding: 16px;
                    overflow-y: auto;
                    max-height: 300px;
                }

                .xlnc-message {
                    margin-bottom: 12px;
                    display: flex;
                    gap: 8px;
                }

                .xlnc-message.user {
                    flex-direction: row-reverse;
                }

                .xlnc-message-bubble {
                    max-width: 80%;
                    padding: 10px 14px;
                    border-radius: 16px;
                    font-size: 14px;
                    line-height: 1.4;
                }

                .xlnc-message.bot .xlnc-message-bubble {
                    background: #f1f3f4;
                    color: #333;
                }

                .xlnc-message.user .xlnc-message-bubble {
                    background: #00d084;
                    color: white;
                }

                .xlnc-typing-indicator {
                    display: flex;
                    gap: 4px;
                    padding: 10px 14px;
                    background: #f1f3f4;
                    border-radius: 16px;
                    max-width: 80px;
                }

                .xlnc-typing-dot {
                    width: 6px;
                    height: 6px;
                    background: #999;
                    border-radius: 50%;
                    animation: typing 1.4s infinite;
                }

                .xlnc-typing-dot:nth-child(2) {
                    animation-delay: 0.2s;
                }

                .xlnc-typing-dot:nth-child(3) {
                    animation-delay: 0.4s;
                }

                @keyframes typing {
                    0%, 60%, 100% {
                        transform: translateY(0);
                    }
                    30% {
                        transform: translateY(-10px);
                    }
                }

                .xlnc-chat-input-container {
                    padding: 16px;
                    border-top: 1px solid #e1e5e9;
                    display: flex;
                    gap: 8px;
                }

                .xlnc-chat-input {
                    flex: 1;
                    padding: 10px;
                    border: 2px solid #e1e5e9;
                    border-radius: 20px;
                    font-size: 14px;
                    outline: none;
                    transition: border-color 0.2s;
                }

                .xlnc-chat-input:focus {
                    border-color: #00d084;
                }

                .xlnc-send-btn {
                    background: #00d084;
                    color: white;
                    border: none;
                    padding: 10px 16px;
                    border-radius: 20px;
                    cursor: pointer;
                    transition: background 0.2s;
                }

                .xlnc-send-btn:hover {
                    background: #00b874;
                }

                .xlnc-send-btn:disabled {
                    background: #ccc;
                    cursor: not-allowed;
                }

                .xlnc-footer {
                    padding: 8px 16px;
                    text-align: center;
                    font-size: 11px;
                    color: #666;
                    border-top: 1px solid #e1e5e9;
                }

                .xlnc-menu-dropdown {
                    position: absolute;
                    top: 60px;
                    right: 16px;
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
                    min-width: 200px;
                    z-index: 1000;
                    display: none;
                }

                .xlnc-menu-dropdown.show {
                    display: block;
                    animation: fadeIn 0.2s ease-out;
                }

                @keyframes fadeIn {
                    from { opacity: 0; transform: translateY(-8px); }
                    to { opacity: 1; transform: translateY(0); }
                }

                .xlnc-menu-item {
                    padding: 12px 16px;
                    cursor: pointer;
                    border-bottom: 1px solid #f0f0f0;
                    font-size: 14px;
                    color: #333;
                    transition: background 0.2s;
                }

                .xlnc-menu-item:hover {
                    background: #f8f9fa;
                }

                .xlnc-menu-item:last-child {
                    border-bottom: none;
                }

                .xlnc-quick-actions {
                    display: flex;
                    gap: 8px;
                    margin-top: 12px;
                    flex-wrap: wrap;
                }

                .xlnc-quick-action {
                    background: #f1f3f4;
                    border: none;
                    padding: 8px 12px;
                    border-radius: 16px;
                    font-size: 12px;
                    color: #666;
                    cursor: pointer;
                    transition: all 0.2s;
                }

                .xlnc-quick-action:hover {
                    background: #e1e5e9;
                    color: #333;
                }

                /* Mobile Responsive */
                @media (max-width: 768px) {
                    .xlnc-chat-window {
                        width: calc(100vw - 20px);
                        height: calc(100vh - 100px);
                        position: fixed;
                        bottom: 80px;
                        right: 10px;
                        left: 10px;
                        margin: 0;
                    }

                    .xlnc-chat-widget {
                        bottom: 20px;
                        right: 20px;
                    }
                }

                @media (max-width: 480px) {
                    .xlnc-chat-window {
                        width: 100vw;
                        height: 100vh;
                        position: fixed;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        border-radius: 0;
                        margin: 0;
                    }

                    .xlnc-chat-widget {
                        bottom: 20px;
                        right: 20px;
                    }
                }
            `;

            const styleSheet = document.createElement('style');
            styleSheet.textContent = styles;
            document.head.appendChild(styleSheet);
        }

        getPositionStyles() {
            const position = this.config.position || 'bottom-right';
            const positions = {
                'bottom-right': 'bottom: 20px; right: 20px;',
                'bottom-left': 'bottom: 20px; left: 20px;',
                'top-right': 'top: 20px; right: 20px;',
                'top-left': 'top: 20px; left: 20px;'
            };
            return positions[position] || positions['bottom-right'];
        }

        createWidget() {
            const widget = document.createElement('div');
            widget.className = 'xlnc-chat-widget';
            widget.innerHTML = `
                <div class="xlnc-chat-window" id="xlnc-chat-window">
                    <div class="xlnc-chat-header">
                        <h3>${this.config.companyName}</h3>
                        <div class="xlnc-chat-controls">
                            <button class="xlnc-chat-menu-btn" id="xlnc-menu-btn">⋮</button>
                            <button class="xlnc-chat-close-btn" id="xlnc-close-btn">×</button>
                        </div>
                    </div>
                    
                    <div class="xlnc-chat-body">
                        <div class="xlnc-greeting-view" id="xlnc-greeting-view">
                            <p class="xlnc-greeting-text">${this.config.greeting}</p>
                            <button class="xlnc-start-chat-btn" id="xlnc-start-chat">Start Chat</button>
                        </div>

                        <div class="xlnc-form-view" id="xlnc-form-view" style="display: none;">
                            <form id="xlnc-chat-form">
                                <div class="xlnc-form-field">
                                    <label for="xlnc-name">Name *</label>
                                    <input type="text" id="xlnc-name" name="name" required>
                                    <div class="xlnc-form-error" id="xlnc-name-error"></div>
                                </div>
                                
                                <div class="xlnc-form-field">
                                    <label for="xlnc-email">Email *</label>
                                    <input type="email" id="xlnc-email" name="email" required>
                                    <div class="xlnc-form-error" id="xlnc-email-error"></div>
                                </div>
                                
                                <div class="xlnc-form-field">
                                    <label for="xlnc-phone">Phone *</label>
                                    <div class="xlnc-phone-container">
                                        <select class="xlnc-country-code" id="xlnc-country-code">
                                            <option value="+1">+1</option>
                                            <option value="+44">+44</option>
                                            <option value="+91">+91</option>
                                            <option value="+61">+61</option>
                                            <option value="+81">+81</option>
                                        </select>
                                        <input type="tel" id="xlnc-phone" name="phone" required>
                                    </div>
                                    <div class="xlnc-form-error" id="xlnc-phone-error"></div>
                                </div>
                                
                                <button type="submit" class="xlnc-submit-btn">Start Conversation</button>
                            </form>
                        </div>

                        <div class="xlnc-chat-view" id="xlnc-chat-view" style="display: none;">
                            <div class="xlnc-chat-messages" id="xlnc-chat-messages"></div>
                            <div class="xlnc-chat-input-container">
                                <input type="text" class="xlnc-chat-input" id="xlnc-chat-input" placeholder="Type your message...">
                                <button class="xlnc-send-btn" id="xlnc-send-btn">Send</button>
                            </div>
                        </div>
                    </div>

                    <div class="xlnc-footer">
                        Powered by tawk.to
                    </div>

                    <div class="xlnc-menu-dropdown" id="xlnc-menu-dropdown">
                        <div class="xlnc-menu-item" data-action="change-name">Change Name</div>
                        <div class="xlnc-menu-item" data-action="email-transcript">Email transcript</div>
                        <div class="xlnc-menu-item" data-action="sound-toggle">Sound On</div>
                        <div class="xlnc-menu-item" data-action="end-chat">End this chat session</div>
                        <div class="xlnc-menu-item" data-action="add-widget">Add Chat to your website</div>
                    </div>
                </div>

                <div class="xlnc-chat-bubble" id="xlnc-chat-bubble">
                    <svg viewBox="0 0 24 24">
                        <path d="M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M6,9V7H18V9H6M14,11V13H6V11H14M16,15V17H6V15H16Z" />
                    </svg>
                    <div class="xlnc-chat-notification" id="xlnc-notification" style="display: none;">1</div>
                </div>
            `;

            document.body.appendChild(widget);
            this.widget = widget;
        }

        bindEvents() {
            const bubble = document.getElementById('xlnc-chat-bubble');
            const closeBtn = document.getElementById('xlnc-close-btn');
            const startChatBtn = document.getElementById('xlnc-start-chat');
            const form = document.getElementById('xlnc-chat-form');
            const sendBtn = document.getElementById('xlnc-send-btn');
            const chatInput = document.getElementById('xlnc-chat-input');
            const menuBtn = document.getElementById('xlnc-menu-btn');
            const menuDropdown = document.getElementById('xlnc-menu-dropdown');

            bubble.addEventListener('click', () => this.toggleWidget());
            closeBtn.addEventListener('click', () => this.closeWidget());
            startChatBtn.addEventListener('click', () => this.showForm());
            form.addEventListener('submit', (e) => this.handleFormSubmit(e));
            sendBtn.addEventListener('click', () => this.sendMessage());
            chatInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') this.sendMessage();
            });
            menuBtn.addEventListener('click', () => this.toggleMenu());

            // Menu actions
            menuDropdown.addEventListener('click', (e) => {
                if (e.target.classList.contains('xlnc-menu-item')) {
                    this.handleMenuAction(e.target.dataset.action);
                }
            });

            // Click outside to close menu
            document.addEventListener('click', (e) => {
                if (!menuBtn.contains(e.target) && !menuDropdown.contains(e.target)) {
                    menuDropdown.classList.remove('show');
                }
            });
        }

        toggleWidget() {
            const window = document.getElementById('xlnc-chat-window');
            const notification = document.getElementById('xlnc-notification');
            
            if (this.state.isOpen) {
                this.closeWidget();
            } else {
                window.classList.add('open');
                this.state.isOpen = true;
                notification.style.display = 'none';
                
                // Show appropriate view based on state
                if (this.state.chatHistory.length > 0 && this.state.userInfo) {
                    this.showChatView();
                }
            }
        }

        closeWidget() {
            const window = document.getElementById('xlnc-chat-window');
            window.classList.remove('open');
            this.state.isOpen = false;
        }

        showForm() {
            document.getElementById('xlnc-greeting-view').style.display = 'none';
            document.getElementById('xlnc-form-view').style.display = 'block';
            this.state.currentView = 'form';
        }

        showChatView() {
            document.getElementById('xlnc-greeting-view').style.display = 'none';
            document.getElementById('xlnc-form-view').style.display = 'none';
            document.getElementById('xlnc-chat-view').style.display = 'flex';
            this.state.currentView = 'chat';
            this.renderChatHistory();
        }

        handleFormSubmit(e) {
            e.preventDefault();
            
            const formData = {
                name: document.getElementById('xlnc-name').value.trim(),
                email: document.getElementById('xlnc-email').value.trim(),
                phone: document.getElementById('xlnc-country-code').value + document.getElementById('xlnc-phone').value.trim()
            };

            if (this.validateForm(formData)) {
                this.state.userInfo = formData;
                this.saveChatHistory();
                this.showChatView();
                this.authenticateSession();
                this.addMessage('bot', `Hello ${formData.name}! How can I assist you today?`);
                this.showQuickActions();
            }
        }

        validateForm(formData) {
            let isValid = true;
            
            // Clear previous errors
            document.querySelectorAll('.xlnc-form-error').forEach(el => el.textContent = '');
            document.querySelectorAll('.xlnc-form-field').forEach(el => el.classList.remove('error'));

            if (!formData.name) {
                this.showFieldError('name', 'This field is required');
                isValid = false;
            }

            if (!formData.email) {
                this.showFieldError('email', 'This field is required');
                isValid = false;
            } else if (!this.isValidEmail(formData.email)) {
                this.showFieldError('email', 'Please enter a valid email address');
                isValid = false;
            }

            if (!document.getElementById('xlnc-phone').value.trim()) {
                this.showFieldError('phone', 'This field is required');
                isValid = false;
            }

            return isValid;
        }

        showFieldError(fieldName, message) {
            const errorEl = document.getElementById(`xlnc-${fieldName}-error`);
            const fieldEl = document.getElementById(`xlnc-${fieldName}`).parentElement;
            
            errorEl.textContent = message;
            fieldEl.classList.add('error');
        }

        isValidEmail(email) {
            return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
        }

        async authenticateSession() {
            try {
                // Simulate API call for authentication
                const response = await fetch('/api/chat/authenticate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${this.config.key}`,
                        'X-Secret': this.config.secret
                    },
                    body: JSON.stringify(this.state.userInfo)
                });

                if (response.ok) {
                    const data = await response.json();
                    this.state.sessionId = data.sessionId;
                } else {
                    // Use demo session ID for now
                    this.state.sessionId = 'demo_session_' + Date.now();
                }
            } catch (error) {
                console.log('Using demo mode - API not available');
                this.state.sessionId = 'demo_session_' + Date.now();
            }
        }

        sendMessage() {
            const input = document.getElementById('xlnc-chat-input');
            const message = input.value.trim();
            
            if (!message) return;

            this.addMessage('user', message);
            input.value = '';
            
            // Show typing indicator
            this.showTypingIndicator();
            
            // Process message and generate response
            setTimeout(() => {
                this.hideTypingIndicator();
                this.processUserMessage(message);
            }, 1000);
        }

        addMessage(sender, message, actions = null) {
            const messagesContainer = document.getElementById('xlnc-chat-messages');
            const messageEl = document.createElement('div');
            messageEl.className = `xlnc-message ${sender}`;
            
            let messageContent = `<div class="xlnc-message-bubble">${message}</div>`;
            
            if (actions) {
                messageContent += '<div class="xlnc-quick-actions">';
                actions.forEach(action => {
                    messageContent += `<button class="xlnc-quick-action" onclick="window.xlncWidget.handleQuickAction('${action.id}')">${action.text}</button>`;
                });
                messageContent += '</div>';
            }
            
            messageEl.innerHTML = messageContent;
            messagesContainer.appendChild(messageEl);
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
            
            // Save to chat history
            this.state.chatHistory.push({
                sender,
                message,
                timestamp: new Date().toISOString(),
                actions
            });
            this.saveChatHistory();
        }

        showTypingIndicator() {
            const messagesContainer = document.getElementById('xlnc-chat-messages');
            const typingEl = document.createElement('div');
            typingEl.className = 'xlnc-message bot';
            typingEl.id = 'xlnc-typing-indicator';
            typingEl.innerHTML = `
                <div class="xlnc-typing-indicator">
                    <div class="xlnc-typing-dot"></div>
                    <div class="xlnc-typing-dot"></div>
                    <div class="xlnc-typing-dot"></div>
                </div>
            `;
            messagesContainer.appendChild(typingEl);
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
            this.state.isTyping = true;
        }

        hideTypingIndicator() {
            const typingEl = document.getElementById('xlnc-typing-indicator');
            if (typingEl) {
                typingEl.remove();
            }
            this.state.isTyping = false;
        }

        processUserMessage(message) {
            const lowerMessage = message.toLowerCase();
            let response = '';
            let actions = null;

            // Check knowledge base first
            const kbResponse = this.searchKnowledgeBase(lowerMessage);
            if (kbResponse) {
                response = kbResponse;
            }
            // Check for meeting scheduling intent
            else if (this.isMeetingRequest(lowerMessage)) {
                response = "I'd be happy to help you schedule a meeting! Please choose a preferred time slot:";
                actions = [
                    { id: 'schedule_today', text: 'Today' },
                    { id: 'schedule_tomorrow', text: 'Tomorrow' },
                    { id: 'schedule_next_week', text: 'Next Week' },
                    { id: 'schedule_custom', text: 'Custom Date' }
                ];
            }
            // Default AI response
            else {
                response = this.generateAIResponse(message);
                // If AI couldn't help, offer meeting
                if (response.includes("I don't have specific information")) {
                    actions = [
                        { id: 'schedule_meeting', text: 'Schedule a Meeting' },
                        { id: 'contact_support', text: 'Contact Support' }
                    ];
                }
            }

            this.addMessage('bot', response, actions);
        }

        searchKnowledgeBase(query) {
            for (const [key, value] of Object.entries(this.knowledgeBase)) {
                if (query.includes(key) || this.fuzzyMatch(query, key)) {
                    return value;
                }
            }
            return null;
        }

        fuzzyMatch(text, keyword) {
            const synonyms = {
                'services': ['service', 'offer', 'provide', 'do'],
                'pricing': ['price', 'cost', 'rate', 'fee', 'budget'],
                'support': ['help', 'assistance', 'issue', 'problem'],
                'contact': ['reach', 'call', 'email', 'phone'],
                'hours': ['time', 'open', 'available', 'schedule'],
                'technologies': ['tech', 'tools', 'framework', 'language']
            };

            if (synonyms[keyword]) {
                return synonyms[keyword].some(synonym => text.includes(synonym));
            }
            return false;
        }

        isMeetingRequest(message) {
            const meetingKeywords = ['meeting', 'schedule', 'appointment', 'call', 'demo', 'consultation', 'discuss'];
            return meetingKeywords.some(keyword => message.includes(keyword));
        }

        generateAIResponse(message) {
            // Simple AI response logic - in production, this would call an actual AI API
            const responses = [
                "I understand your question. Let me help you with that.",
                "That's a great question! Based on our expertise, I can tell you that...",
                "I'd be happy to assist you with that inquiry.",
                "Thank you for your question. Here's what I can share with you..."
            ];

            if (message.length < 10) {
                return "Could you please provide more details about what you're looking for?";
            }

            if (message.includes('?')) {
                return responses[Math.floor(Math.random() * responses.length)] + " However, I don't have specific information about that topic in my knowledge base. Would you like to schedule a meeting with our experts?";
            }

            return "Thank you for reaching out! I'd be happy to help you. Could you please provide more specific details about what you're looking for?";
        }

        handleQuickAction(actionId) {
            switch (actionId) {
                case 'schedule_meeting':
                case 'schedule_today':
                case 'schedule_tomorrow':
                case 'schedule_next_week':
                case 'schedule_custom':
                    this.handleMeetingScheduling(actionId);
                    break;
                case 'contact_support':
                    this.addMessage('bot', 'You can reach our support team at support@xlnc.com or call +1-555-0123. We\'re available 24/7 for enterprise clients.');
                    break;
                default:
                    this.addMessage('bot', 'Thank you for your selection. How else can I assist you?');
            }
        }

        handleMeetingScheduling(timeSlot) {
            let message = "Great! I'm setting up a meeting for you. ";
            
            switch (timeSlot) {
                case 'schedule_today':
                    message += "For today, our available slots are 2:00 PM, 3:30 PM, and 5:00 PM EST.";
                    break;
                case 'schedule_tomorrow':
                    message += "For tomorrow, we have openings at 10:00 AM, 1:00 PM, and 4:00 PM EST.";
                    break;
                case 'schedule_next_week':
                    message += "Next week we have several openings. Monday through Friday, 9 AM to 5 PM EST.";
                    break;
                default:
                    message += "Please let me know your preferred date and time, and I'll check our availability.";
            }

            message += " I'll send you a calendar invitation once you confirm the time. What works best for you?";
            
            this.addMessage('bot', message);
            
            // In a real implementation, this would integrate with a calendar API
            this.scheduleCalendarMeeting(timeSlot);
        }

        async scheduleCalendarMeeting(timeSlot) {
            // Simulate calendar integration
            setTimeout(() => {
                const meetingId = 'xlnc_meeting_' + Date.now();
                this.addMessage('bot', `Perfect! I've created meeting ID: ${meetingId}. You'll receive a calendar invitation shortly at ${this.state.userInfo.email}. Is there anything specific you'd like to discuss in the meeting?`);
            }, 2000);
        }

        showQuickActions() {
            const actions = [
                { id: 'services_info', text: 'Our Services' },
                { id: 'pricing_info', text: 'Pricing' },
                { id: 'schedule_meeting', text: 'Schedule Meeting' }
            ];
            
            this.addMessage('bot', 'Here are some quick options to get you started:', actions);
        }

        toggleMenu() {
            const dropdown = document.getElementById('xlnc-menu-dropdown');
            dropdown.classList.toggle('show');
        }

        handleMenuAction(action) {
            const dropdown = document.getElementById('xlnc-menu-dropdown');
            dropdown.classList.remove('show');

            switch (action) {
                case 'change-name':
                    this.addMessage('bot', 'Name change functionality will be available soon.');
                    break;
                case 'email-transcript':
                    this.emailTranscript();
                    break;
                case 'sound-toggle':
                    this.addMessage('bot', 'Sound settings will be available soon.');
                    break;
                case 'end-chat':
                    this.endChatSession();
                    break;
                case 'add-widget':
                    this.addMessage('bot', 'Widget integration guide will be available soon.');
                    break;
            }
        }

        emailTranscript() {
            if (this.state.userInfo && this.state.chatHistory.length > 0) {
                // Simulate email sending
                this.addMessage('bot', `Chat transcript has been sent to ${this.state.userInfo.email}. Thank you!`);
            } else {
                this.addMessage('bot', 'No chat history available to send.');
            }
        }

        endChatSession() {
            this.state.chatHistory = [];
            this.state.userInfo = null;
            this.state.sessionId = null;
            this.saveChatHistory();
            
            document.getElementById('xlnc-chat-view').style.display = 'none';
            document.getElementById('xlnc-greeting-view').style.display = 'block';
            document.getElementById('xlnc-chat-messages').innerHTML = '';
            this.state.currentView = 'greeting';
            
            this.addMessage('bot', 'Chat session ended. Thank you for contacting us!');
        }

        renderChatHistory() {
            const messagesContainer = document.getElementById('xlnc-chat-messages');
            messagesContainer.innerHTML = '';
            
            this.state.chatHistory.forEach(msg => {
                this.addMessage(msg.sender, msg.message, msg.actions);
            });
        }

        loadChatHistory() {
            try {
                const history = localStorage.getItem('xlnc_chat_history');
                return history ? JSON.parse(history) : [];
            } catch {
                return [];
            }
        }

        saveChatHistory() {
            try {
                localStorage.setItem('xlnc_chat_history', JSON.stringify(this.state.chatHistory));
                if (this.state.userInfo) {
                    localStorage.setItem('xlnc_user_info', JSON.stringify(this.state.userInfo));
                }
            } catch (error) {
                console.warn('Could not save chat history to localStorage');
            }
        }

        handleResponsive() {
            const checkMobile = () => {
                const isMobile = window.innerWidth <= 768;
                const widget = document.querySelector('.xlnc-chat-widget');
                
                if (isMobile) {
                    widget.classList.add('mobile');
                } else {
                    widget.classList.remove('mobile');
                }
            };

            checkMobile();
            window.addEventListener('resize', checkMobile);
        }
    }

    // Initialize widget when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            window.xlncWidget = new ChatWidget();
        });
    } else {
        window.xlncWidget = new ChatWidget();
    }

    // Global access for quick actions
    window.xlncWidget = window.xlncWidget || {};
})();