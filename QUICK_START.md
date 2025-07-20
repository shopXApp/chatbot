# 🚀 Quick Start Guide - XLNC Chat Widget

## Instant Setup (2 minutes)

### 1. Download Files
```bash
# Clone or download these files:
- tawkWidget.js (main widget)
- widget-config.js (optional configuration)
- api-integration.js (optional API features)
```

### 2. Basic Integration
Add this single line to your HTML before `</body>`:

```html
<script 
    src="tawkWidget.js" 
    key="your_api_key" 
    secret="your_secret_key">
</script>
```

### 3. Test Locally
```bash
# Start local server
python3 -m http.server 8000

# Or with Node.js
npx serve .

# Open browser to test
http://localhost:8000/test.html
```

## Live Demo Features

🎯 **What to Test:**
- ✅ Multiple themes (Green, Blue, Purple, Dark)
- ✅ Different positions (corners & sides)
- ✅ Pre-chat form with validation
- ✅ AI knowledge base responses
- ✅ Meeting scheduling workflow
- ✅ Mobile responsive design
- ✅ Chat history persistence
- ✅ Quick action buttons

## Advanced Configuration

### Custom Greeting & Position
```html
<script 
    src="tawkWidget.js" 
    key="your_key" 
    secret="your_secret"
    greeting="Welcome! How can we help you today?"
    position="bottom-left"
    theme="blue"
    company="Your Company">
</script>
```

### Multiple Themes Available
- `green` (default) - #00d084
- `blue` - #0084ff  
- `purple` - #7c3aed
- `dark` - #1f2937

### All Position Options
- `bottom-right`, `bottom-left`
- `top-right`, `top-left`
- `middle-right`, `middle-left`

## Knowledge Base Setup

The widget includes built-in responses for:
- **Services** - Web development, mobile apps, cloud solutions
- **Pricing** - Cost information and quotes  
- **Support** - Technical assistance
- **Contact** - Contact information
- **Hours** - Business hours
- **Technologies** - Technical capabilities

## API Integration (Optional)

### Backend Endpoints to Implement
```javascript
POST /api/chat/authenticate     // User authentication
POST /api/chat/message         // Send/receive messages
POST /api/knowledge/search     // Knowledge base search
POST /api/calendar/schedule    // Meeting scheduling
POST /api/whatsapp/send       // WhatsApp integration
```

### Example API Response
```json
{
  "response": "Thank you for your question! Here's what I can help with...",
  "actions": [
    {"id": "schedule_meeting", "text": "Schedule Meeting"},
    {"id": "contact_support", "text": "Contact Support"}
  ],
  "source": "knowledge_base",
  "confidence": 0.9
}
```

## Customization Examples

### Custom CSS Styling
```html
<style>
.xlnc-chat-bubble {
    background: linear-gradient(45deg, #ff6b6b, #ee5a52) !important;
}
.xlnc-chat-header {
    background: #2c3e50 !important;
}
</style>
```

### Custom Knowledge Base
```javascript
// In widget-config.js
knowledgeBase: {
    categories: {
        'products': {
            keywords: ['product', 'item', 'buy'],
            responses: ['We offer amazing products...']
        }
    }
}
```

## Mobile Optimization

- **Automatic responsive design**
- **Full-screen mode** on phones
- **Touch-friendly** controls
- **Smooth animations**

## Browser Support

✅ Chrome 60+  
✅ Firefox 55+  
✅ Safari 12+  
✅ Edge 79+  
✅ Mobile browsers

## Production Checklist

- [ ] Replace demo keys with real API credentials
- [ ] Set up backend API endpoints
- [ ] Configure knowledge base content
- [ ] Test on mobile devices
- [ ] Add custom styling if needed
- [ ] Set up analytics tracking
- [ ] Configure WhatsApp integration
- [ ] Test meeting scheduling flow

## Support & Documentation

📖 **Full Documentation**: `README.md`  
🧪 **Live Demo**: `test.html`  
⚙️ **Configuration**: `widget-config.js`  
🔌 **API Integration**: `api-integration.js`  

## Common Use Cases

### E-commerce Site
```html
<script src="tawkWidget.js" 
    key="ecommerce_key" 
    greeting="Welcome to our store! Need help finding products?"
    theme="blue">
</script>
```

### SaaS Platform  
```html
<script src="tawkWidget.js" 
    key="saas_key"
    greeting="Hi! Need help with our platform?"
    theme="purple">
</script>
```

### Service Business
```html
<script src="tawkWidget.js" 
    key="service_key"
    greeting="Hello! How can we assist you today?"
    theme="green">
</script>
```

## Next Steps

1. **Test the demo** at `test.html`
2. **Read full docs** in `README.md`
3. **Customize themes** in `widget-config.js`
4. **Set up APIs** using `api-integration.js`
5. **Deploy to production** with your credentials

---

**Need Help?** Check the full README.md or contact support@xlnc.com