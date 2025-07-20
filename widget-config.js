// XLNC Chat Widget Configuration
window.XLNCChatConfig = {
    // Default Widget Configuration
    defaults: {
        // Authentication
        key: 'demo_key',
        secret: 'demo_secret',
        
        // API Endpoints
        apiURL: 'https://api.xlnc.com',
        whatsAppAPI: 'https://graph.facebook.com/v18.0',
        
        // Appearance
        theme: 'green',
        position: 'bottom-right',
        companyName: 'XLNC Technologies',
        
        // Messages
        greeting: 'Hello! Greetings from XLNC Technologies! We offer a wide range of IT services designed to meet the diverse needs of businesses across various industries.',
        offlineMessage: 'We\'re currently offline. Please leave your message and we\'ll get back to you soon.',
        
        // Features
        enableWhatsApp: true,
        enableCalendarScheduling: true,
        enableFileUpload: false,
        enableVoiceMessages: false,
        enableTypingIndicator: true,
        enableReadReceipts: true,
        
        // Behavior
        autoOpen: false,
        autoOpenDelay: 5000, // milliseconds
        showNotifications: true,
        playMessageSounds: true,
        persistChatHistory: true,
        
        // Customization
        customCSS: null,
        customActions: [],
        
        // Analytics
        enableAnalytics: true,
        trackUserInteractions: true,
        
        // Mobile
        mobileFullScreen: true,
        mobileSlideUp: true
    },

    // Theme Configurations
    themes: {
        green: {
            primaryColor: '#00d084',
            primaryColorHover: '#00b874',
            secondaryColor: '#f1f3f4',
            textColor: '#333333',
            backgroundColor: '#ffffff',
            headerColor: '#00d084',
            headerTextColor: '#ffffff',
            bubbleGradient: 'linear-gradient(135deg, #00d084, #00b874)',
            borderRadius: '12px',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
        },
        blue: {
            primaryColor: '#0084ff',
            primaryColorHover: '#006bd6',
            secondaryColor: '#f0f8ff',
            textColor: '#333333',
            backgroundColor: '#ffffff',
            headerColor: '#0084ff',
            headerTextColor: '#ffffff',
            bubbleGradient: 'linear-gradient(135deg, #0084ff, #006bd6)',
            borderRadius: '12px',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
        },
        purple: {
            primaryColor: '#7c3aed',
            primaryColorHover: '#6d28d9',
            secondaryColor: '#f3f4f6',
            textColor: '#333333',
            backgroundColor: '#ffffff',
            headerColor: '#7c3aed',
            headerTextColor: '#ffffff',
            bubbleGradient: 'linear-gradient(135deg, #7c3aed, #6d28d9)',
            borderRadius: '12px',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
        },
        dark: {
            primaryColor: '#1f2937',
            primaryColorHover: '#111827',
            secondaryColor: '#374151',
            textColor: '#ffffff',
            backgroundColor: '#1f2937',
            headerColor: '#111827',
            headerTextColor: '#ffffff',
            bubbleGradient: 'linear-gradient(135deg, #374151, #1f2937)',
            borderRadius: '12px',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
        }
    },

    // Position Options
    positions: {
        'bottom-right': { bottom: '20px', right: '20px' },
        'bottom-left': { bottom: '20px', left: '20px' },
        'top-right': { top: '20px', right: '20px' },
        'top-left': { top: '20px', left: '20px' },
        'middle-right': { top: '50%', right: '20px', transform: 'translateY(-50%)' },
        'middle-left': { top: '50%', left: '20px', transform: 'translateY(-50%)' }
    },

    // Responsive Breakpoints
    breakpoints: {
        mobile: 480,
        tablet: 768,
        desktop: 1024
    },

    // Knowledge Base Categories
    knowledgeBase: {
        categories: {
            'services': {
                keywords: ['service', 'offer', 'provide', 'do', 'capabilities'],
                responses: [
                    'We offer web development, mobile app development, cloud solutions, AI integration, and digital transformation services.',
                    'Our comprehensive IT services include custom software development, cloud migration, cybersecurity, and 24/7 technical support.',
                    'We specialize in modern web technologies, mobile applications, enterprise solutions, and AI-powered business automation.'
                ]
            },
            'pricing': {
                keywords: ['price', 'cost', 'rate', 'fee', 'budget', 'quote'],
                responses: [
                    'Our pricing varies based on project requirements. Please schedule a consultation for a detailed quote.',
                    'We offer competitive pricing with flexible payment options. Contact us for a customized quote based on your specific needs.',
                    'Project costs depend on scope and complexity. We provide transparent pricing with no hidden fees.'
                ]
            },
            'support': {
                keywords: ['help', 'assistance', 'issue', 'problem', 'support', 'bug'],
                responses: [
                    'We provide 24/7 technical support for all our enterprise clients.',
                    'Our support team is available around the clock to assist with any technical issues.',
                    'We offer multiple support channels including chat, email, and phone support.'
                ]
            },
            'contact': {
                keywords: ['reach', 'call', 'email', 'phone', 'contact', 'address'],
                responses: [
                    'You can reach us at info@xlnc.com or call +1-555-0123.',
                    'Contact us via email at info@xlnc.com or schedule a call through our website.',
                    'Our main office number is +1-555-0123, or you can email us at info@xlnc.com.'
                ]
            },
            'hours': {
                keywords: ['time', 'open', 'available', 'schedule', 'hours', 'when'],
                responses: [
                    'Our business hours are Monday to Friday, 9 AM to 6 PM EST.',
                    'We\'re available Monday through Friday from 9:00 AM to 6:00 PM Eastern Time.',
                    'Office hours: 9 AM - 6 PM EST, Monday to Friday. Emergency support available 24/7.'
                ]
            },
            'technologies': {
                keywords: ['tech', 'tools', 'framework', 'language', 'platform', 'stack'],
                responses: [
                    'We work with React, Angular, Node.js, .NET Core, Python, AWS, Azure, and many other modern technologies.',
                    'Our technology stack includes modern frameworks like React, Vue.js, Angular, backend technologies like Node.js, Python, .NET, and cloud platforms like AWS and Azure.',
                    'We specialize in cutting-edge technologies including AI/ML frameworks, cloud-native development, and modern web technologies.'
                ]
            }
        }
    },

    // Quick Actions Configuration
    quickActions: {
        default: [
            { id: 'services_info', text: 'Our Services', icon: '🛠️' },
            { id: 'pricing_info', text: 'Pricing', icon: '💰' },
            { id: 'schedule_meeting', text: 'Schedule Meeting', icon: '📅' },
            { id: 'contact_support', text: 'Contact Support', icon: '💬' }
        ],
        meeting: [
            { id: 'schedule_today', text: 'Today', icon: '📅' },
            { id: 'schedule_tomorrow', text: 'Tomorrow', icon: '📅' },
            { id: 'schedule_next_week', text: 'Next Week', icon: '📅' },
            { id: 'schedule_custom', text: 'Custom Date', icon: '🗓️' }
        ]
    },

    // Animation Settings
    animations: {
        slideUp: {
            duration: '0.3s',
            easing: 'ease-out',
            transform: 'translateY(20px)'
        },
        fadeIn: {
            duration: '0.2s',
            easing: 'ease-out',
            opacity: '0'
        },
        bounce: {
            duration: '0.6s',
            easing: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
            transform: 'scale(0.8)'
        }
    },

    // Validation Rules
    validation: {
        email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        phone: /^[\+]?[1-9][\d]{0,15}$/,
        name: /^[a-zA-Z\s]{2,50}$/
    },

    // Error Messages
    errorMessages: {
        required: 'This field is required',
        invalidEmail: 'Please enter a valid email address',
        invalidPhone: 'Please enter a valid phone number',
        invalidName: 'Please enter a valid name (2-50 characters)',
        networkError: 'Network error. Please check your connection and try again.',
        serverError: 'Server error. Please try again later.',
        sessionExpired: 'Session expired. Please refresh the page.'
    },

    // Country Codes for Phone Numbers
    countryCodes: [
        { code: '+1', country: 'US/Canada', flag: '🇺🇸' },
        { code: '+44', country: 'United Kingdom', flag: '🇬🇧' },
        { code: '+91', country: 'India', flag: '🇮🇳' },
        { code: '+61', country: 'Australia', flag: '🇦🇺' },
        { code: '+81', country: 'Japan', flag: '🇯🇵' },
        { code: '+49', country: 'Germany', flag: '🇩🇪' },
        { code: '+33', country: 'France', flag: '🇫🇷' },
        { code: '+86', country: 'China', flag: '🇨🇳' },
        { code: '+55', country: 'Brazil', flag: '🇧🇷' },
        { code: '+52', country: 'Mexico', flag: '🇲🇽' }
    ],

    // Utility Functions
    utils: {
        // Merge configurations
        mergeConfig: function(defaultConfig, userConfig) {
            return Object.assign({}, defaultConfig, userConfig);
        },

        // Generate unique ID
        generateId: function() {
            return 'xlnc_' + Math.random().toString(36).substr(2, 9);
        },

        // Format timestamp
        formatTimestamp: function(timestamp) {
            const date = new Date(timestamp);
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        },

        // Sanitize HTML
        sanitizeHTML: function(html) {
            const div = document.createElement('div');
            div.textContent = html;
            return div.innerHTML;
        },

        // Debounce function
        debounce: function(func, wait) {
            let timeout;
            return function executedFunction(...args) {
                const later = () => {
                    clearTimeout(timeout);
                    func(...args);
                };
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
            };
        },

        // Throttle function
        throttle: function(func, limit) {
            let inThrottle;
            return function() {
                const args = arguments;
                const context = this;
                if (!inThrottle) {
                    func.apply(context, args);
                    inThrottle = true;
                    setTimeout(() => inThrottle = false, limit);
                }
            };
        },

        // Check mobile device
        isMobile: function() {
            return window.innerWidth <= this.breakpoints.mobile;
        },

        // Check tablet device
        isTablet: function() {
            return window.innerWidth <= this.breakpoints.tablet && window.innerWidth > this.breakpoints.mobile;
        },

        // Get browser info
        getBrowserInfo: function() {
            const ua = navigator.userAgent;
            let browser = 'Unknown';
            
            if (ua.includes('Chrome')) browser = 'Chrome';
            else if (ua.includes('Firefox')) browser = 'Firefox';
            else if (ua.includes('Safari')) browser = 'Safari';
            else if (ua.includes('Edge')) browser = 'Edge';
            
            return {
                browser: browser,
                userAgent: ua,
                language: navigator.language,
                platform: navigator.platform
            };
        }
    }
};

// Export configuration
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.XLNCChatConfig;
}