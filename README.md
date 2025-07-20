# XLNC Chat Widget

A powerful, customizable chat widget that mimics tawk.to design while integrating with WhatsApp Web API, AI assistance, and calendar scheduling. Built with vanilla JavaScript for maximum compatibility.

## Features

### 🎨 Design & UI
- **Tawk.to-inspired design** with modern, responsive interface
- **Multiple themes** (Green, Blue, Purple, Dark)
- **Customizable positioning** (bottom-right, bottom-left, top-right, top-left)
- **Mobile-optimized** with full-screen mode on small devices
- **Smooth animations** and transitions

### 💬 Chat Functionality
- **Pre-chat form** with validation (Name, Email, Phone)
- **Real-time messaging** with typing indicators
- **Chat history persistence** using localStorage
- **Quick action buttons** for common queries
- **Message timestamps** and read receipts

### 🤖 AI & Knowledge Base
- **Intelligent responses** from built-in knowledge base
- **AI-powered fallback** for unknown queries
- **Context-aware conversations**
- **Escalation to human agents**

### 📅 Meeting Scheduling
- **Calendar integration** for appointment booking
- **Available time slots** checking
- **Automatic calendar invitations**
- **Multiple scheduling options** (today, tomorrow, next week, custom)

### 🔧 Integration & APIs
- **WhatsApp Web API** integration
- **RESTful API** for backend communication
- **Authentication** with key/secret pairs
- **Analytics tracking** and reporting

### 📱 Multi-Channel Support
- WhatsApp integration (current)
- Facebook Messenger (planned)
- Instagram (planned)
- Email notifications (planned)

## Installation

### Basic Installation

1. **Include the widget files** in your HTML:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Your Website</title>
</head>
<body>
    <!-- Your website content -->
    
    <!-- XLNC Chat Widget -->
    <script 
        src="tawkWidget.js" 
        key="your_api_key_here" 
        secret="your_secret_key_here"
        greeting="Welcome to our website! How can we help you today?"
        position="bottom-right"
        theme="green">
    </script>
</body>
</html>
```

### Advanced Installation with Configuration

```html
<!-- Include configuration file (optional) -->
<script src="widget-config.js"></script>

<!-- Include API integration (optional) -->
<script src="api-integration.js"></script>

<!-- Include main widget -->
<script 
    src="tawkWidget.js" 
    key="xlnc_your_unique_key_2024" 
    secret="xlnc_your_secret_hash_xyz789"
    greeting="Hello! Greetings from XLNC Technologies! We offer a wide range of IT services."
    position="bottom-right"
    theme="green"
    company="Your Company Name">
</script>
```

## Configuration

### Script Tag Attributes

| Attribute | Description | Default | Required |
|-----------|-------------|---------|----------|
| `key` | Your API authentication key | `demo_key` | Yes |
| `secret` | Your API secret key | `demo_secret` | Yes |
| `greeting` | Welcome message text | Default greeting | No |
| `position` | Widget position | `bottom-right` | No |
| `theme` | Color theme | `green` | No |
| `company` | Company name | `XLNC Technologies` | No |

### Available Positions
- `bottom-right` (default)
- `bottom-left`
- `top-right`
- `top-left`
- `middle-right`
- `middle-left`

### Available Themes
- `green` (default) - #00d084
- `blue` - #0084ff
- `purple` - #7c3aed
- `dark` - #1f2937

### Advanced Configuration

For advanced customization, modify the `widget-config.js` file:

```javascript
window.XLNCChatConfig = {
    defaults: {
        // API Configuration
        apiURL: 'https://your-api.com',
        
        // Features
        enableWhatsApp: true,
        enableCalendarScheduling: true,
        enableFileUpload: false,
        
        // Behavior
        autoOpen: false,
        autoOpenDelay: 5000,
        persistChatHistory: true,
        
        // Mobile settings
        mobileFullScreen: true
    }
};
```

## API Integration

### Authentication

The widget authenticates using your provided key and secret:

```javascript
// Example API call
fetch('/api/chat/authenticate', {
    method: 'POST',
    headers: {
        'Authorization': 'Bearer your_api_key',
        'X-Secret': 'your_secret_key'
    },
    body: JSON.stringify(userInfo)
});
```

### Backend Endpoints

Your backend should implement these endpoints:

#### Authentication
- **POST** `/api/chat/authenticate`
- **Body**: `{ userInfo, timestamp, userAgent, referrer }`
- **Response**: `{ sessionId, success }`

#### Messaging
- **POST** `/api/chat/message`
- **Headers**: `X-Session-ID`
- **Body**: `{ sessionId, message, context }`
- **Response**: `{ response, actions, source, confidence }`

#### Knowledge Base
- **POST** `/api/knowledge/search`
- **Body**: `{ query, sessionId, limit }`
- **Response**: `{ results, hasMore }`

#### Calendar
- **POST** `/api/calendar/schedule`
- **Body**: `{ sessionId, attendeeEmail, preferredDateTime, purpose }`
- **Response**: `{ meetingId, meetingLink, calendarEvent }`

#### WhatsApp
- **POST** `/api/whatsapp/send`
- **Body**: `{ to, message, sessionId }`
- **Response**: `{ messageId, status }`

## Knowledge Base

### Built-in Categories

The widget includes a pre-configured knowledge base with these categories:

- **Services**: Web development, mobile apps, cloud solutions
- **Pricing**: Cost information and quotes
- **Support**: Technical assistance and help
- **Contact**: Contact information and methods
- **Hours**: Business hours and availability
- **Technologies**: Technical capabilities and tools

### Adding Custom Knowledge

Extend the knowledge base in `widget-config.js`:

```javascript
knowledgeBase: {
    categories: {
        'custom_category': {
            keywords: ['custom', 'specific', 'terms'],
            responses: [
                'Custom response 1',
                'Custom response 2'
            ]
        }
    }
}
```

## Customization

### Custom CSS

Add custom styling by including CSS after the widget:

```html
<style>
.xlnc-chat-widget {
    /* Custom widget styles */
}

.xlnc-chat-bubble {
    background: linear-gradient(135deg, #ff6b6b, #ee5a52) !important;
}

.xlnc-chat-header {
    background: #2c3e50 !important;
}
</style>
```

### Custom Actions

Add custom quick actions:

```javascript
// In widget-config.js
quickActions: {
    custom: [
        { id: 'custom_action', text: 'Custom Action', icon: '⚡' }
    ]
}
```

### Theme Customization

Create custom themes:

```javascript
themes: {
    custom: {
        primaryColor: '#your_color',
        primaryColorHover: '#hover_color',
        backgroundColor: '#bg_color',
        // ... other properties
    }
}
```

## Mobile Optimization

The widget automatically optimizes for mobile devices:

- **Responsive design** adapts to screen sizes
- **Full-screen mode** on devices < 480px width
- **Touch-friendly** controls and interactions
- **Slide-up animation** for better UX

### Mobile Configuration

```javascript
// Disable mobile full-screen
mobileFullScreen: false,

// Custom mobile behavior
mobileSlideUp: true
```

## Analytics & Tracking

### Built-in Analytics

The widget tracks:
- Chat volume and frequency
- User engagement metrics
- Response times
- Popular queries
- Conversion rates

### Custom Analytics

Integrate with your analytics platform:

```javascript
// Example: Google Analytics integration
window.xlncWidget.onMessage = function(message, sender) {
    gtag('event', 'chat_message', {
        'event_category': 'chat',
        'event_label': sender
    });
};
```

## Error Handling

The widget includes comprehensive error handling:

- **Network failures**: Graceful fallback to demo mode
- **API errors**: User-friendly error messages
- **Validation errors**: Real-time form validation
- **Session timeout**: Automatic session renewal

## Browser Support

- **Chrome** 60+
- **Firefox** 55+
- **Safari** 12+
- **Edge** 79+
- **Mobile browsers** (iOS Safari, Chrome Mobile)

## Performance

- **Lightweight**: ~50KB minified
- **Fast loading**: Async initialization
- **Memory efficient**: Optimized DOM manipulation
- **Responsive**: Smooth 60fps animations

## Security

- **XSS protection**: Input sanitization
- **CSRF protection**: Secure API tokens
- **Data validation**: Server-side validation
- **Secure storage**: Encrypted localStorage data

## Troubleshooting

### Common Issues

1. **Widget not appearing**
   - Check console for JavaScript errors
   - Verify script tag attributes
   - Ensure DOM is loaded

2. **API authentication failing**
   - Verify key and secret are correct
   - Check network connectivity
   - Review server-side logs

3. **Mobile display issues**
   - Test viewport meta tag
   - Check responsive CSS
   - Verify touch events

### Debug Mode

Enable debug mode for troubleshooting:

```javascript
window.XLNCChatConfig.debug = true;
```

## Examples

### Basic Implementation

```html
<script 
    src="tawkWidget.js" 
    key="your_key" 
    secret="your_secret">
</script>
```

### E-commerce Site

```html
<script 
    src="tawkWidget.js" 
    key="ecommerce_key_2024" 
    secret="ecommerce_secret_xyz"
    greeting="Welcome to our store! Need help finding products?"
    theme="blue"
    position="bottom-left">
</script>
```

### SaaS Platform

```html
<script 
    src="tawkWidget.js" 
    key="saas_platform_key" 
    secret="saas_secret_hash"
    greeting="Hi! Need help with our platform? I'm here to assist!"
    theme="purple"
    company="SaaS Platform Inc">
</script>
```

## API Reference

### Widget Methods

```javascript
// Access widget instance
const widget = window.xlncWidget;

// Open/close widget
widget.open();
widget.close();

// Send message programmatically
widget.sendMessage('Hello from website');

// Get chat history
const history = widget.getChatHistory();

// End session
widget.endSession();
```

### Events

```javascript
// Widget events
widget.on('open', () => console.log('Widget opened'));
widget.on('close', () => console.log('Widget closed'));
widget.on('message', (message) => console.log('New message:', message));
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For technical support and questions:
- **Email**: support@xlnc.com
- **Documentation**: [https://docs.xlnc.com/chat-widget](https://docs.xlnc.com/chat-widget)
- **GitHub Issues**: [https://github.com/xlnc/chat-widget/issues](https://github.com/xlnc/chat-widget/issues)

## Contributing

We welcome contributions! Please see our contributing guidelines for more information.

## Changelog

### v1.0.0 (Current)
- Initial release
- Basic chat functionality
- Knowledge base integration
- Meeting scheduling
- WhatsApp integration
- Mobile optimization

### Roadmap
- Multi-language support
- Voice messages
- File upload
- Advanced analytics
- Multi-channel integration