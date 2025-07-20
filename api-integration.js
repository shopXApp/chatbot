// API Integration Module for XLNC Chat Widget
class ChatAPI {
    constructor(config) {
        this.config = config;
        this.baseURL = config.apiURL || 'https://api.xlnc.com';
        this.whatsAppAPI = config.whatsAppAPI || 'https://graph.facebook.com/v18.0';
        this.sessionId = null;
        this.authenticated = false;
    }

    // Authenticate with the backend
    async authenticate(userInfo) {
        try {
            const response = await fetch(`${this.baseURL}/api/chat/authenticate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Secret': this.config.secret
                },
                body: JSON.stringify({
                    userInfo,
                    timestamp: new Date().toISOString(),
                    userAgent: navigator.userAgent,
                    referrer: document.referrer
                })
            });

            if (response.ok) {
                const data = await response.json();
                this.sessionId = data.sessionId;
                this.authenticated = true;
                return { success: true, sessionId: data.sessionId };
            } else {
                throw new Error(`Authentication failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Authentication error:', error);
            // Fallback to demo mode
            this.sessionId = 'demo_session_' + Date.now();
            this.authenticated = false;
            return { success: false, sessionId: this.sessionId, error: error.message };
        }
    }

    // Send message to AI/Knowledge Base
    async sendMessage(message, context = {}) {
        try {
            const payload = {
                sessionId: this.sessionId,
                message: message,
                context: {
                    ...context,
                    timestamp: new Date().toISOString(),
                    userAgent: navigator.userAgent
                }
            };

            const response = await fetch(`${this.baseURL}/api/chat/message`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    response: data.response,
                    actions: data.actions || null,
                    source: data.source || 'ai', // 'knowledge_base', 'ai', 'human'
                    confidence: data.confidence || 0.8
                };
            } else {
                throw new Error(`Message send failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Message send error:', error);
            return {
                success: false,
                response: "I'm currently experiencing technical difficulties. Please try again later or schedule a meeting for immediate assistance.",
                actions: [
                    { id: 'schedule_meeting', text: 'Schedule Meeting' },
                    { id: 'contact_support', text: 'Contact Support' }
                ],
                error: error.message
            };
        }
    }

    // Search Knowledge Base
    async searchKnowledgeBase(query) {
        try {
            const response = await fetch(`${this.baseURL}/api/knowledge/search`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`
                },
                body: JSON.stringify({
                    query: query,
                    sessionId: this.sessionId,
                    limit: 5
                })
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    results: data.results,
                    hasMore: data.hasMore || false
                };
            } else {
                throw new Error(`Knowledge base search failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Knowledge base search error:', error);
            return {
                success: false,
                results: [],
                error: error.message
            };
        }
    }

    // Schedule Meeting via Calendar API
    async scheduleMeeting(meetingData) {
        try {
            const payload = {
                sessionId: this.sessionId,
                attendeeEmail: meetingData.email,
                attendeeName: meetingData.name,
                attendeePhone: meetingData.phone,
                preferredDateTime: meetingData.datetime,
                timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
                purpose: meetingData.purpose || 'General consultation',
                duration: meetingData.duration || 30 // minutes
            };

            const response = await fetch(`${this.baseURL}/api/calendar/schedule`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    meetingId: data.meetingId,
                    meetingLink: data.meetingLink,
                    calendarEvent: data.calendarEvent
                };
            } else {
                throw new Error(`Meeting scheduling failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Meeting scheduling error:', error);
            return {
                success: false,
                meetingId: 'demo_meeting_' + Date.now(),
                error: error.message
            };
        }
    }

    // Get Available Time Slots
    async getAvailableSlots(date = null) {
        try {
            const targetDate = date || new Date().toISOString().split('T')[0];
            
            const response = await fetch(`${this.baseURL}/api/calendar/availability?date=${targetDate}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                }
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    slots: data.slots,
                    timezone: data.timezone
                };
            } else {
                throw new Error(`Availability check failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Availability check error:', error);
            // Return demo slots
            return {
                success: false,
                slots: [
                    { time: '10:00', available: true },
                    { time: '14:00', available: true },
                    { time: '16:00', available: false },
                    { time: '17:30', available: true }
                ],
                timezone: 'EST',
                error: error.message
            };
        }
    }

    // Send WhatsApp Message
    async sendWhatsAppMessage(phoneNumber, message) {
        try {
            const response = await fetch(`${this.baseURL}/api/whatsapp/send`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                },
                body: JSON.stringify({
                    to: phoneNumber,
                    message: message,
                    sessionId: this.sessionId
                })
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    messageId: data.messageId,
                    status: data.status
                };
            } else {
                throw new Error(`WhatsApp message failed: ${response.status}`);
            }
        } catch (error) {
            console.error('WhatsApp message error:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Email Chat Transcript
    async emailTranscript(email, chatHistory) {
        try {
            const response = await fetch(`${this.baseURL}/api/chat/transcript`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                },
                body: JSON.stringify({
                    email: email,
                    chatHistory: chatHistory,
                    sessionId: this.sessionId,
                    timestamp: new Date().toISOString()
                })
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    messageId: data.messageId
                };
            } else {
                throw new Error(`Transcript email failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Transcript email error:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Update User Preferences
    async updatePreferences(preferences) {
        try {
            const response = await fetch(`${this.baseURL}/api/user/preferences`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                },
                body: JSON.stringify({
                    sessionId: this.sessionId,
                    preferences: preferences
                })
            });

            if (response.ok) {
                return { success: true };
            } else {
                throw new Error(`Preferences update failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Preferences update error:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Get Analytics Data (for admin dashboard)
    async getAnalytics(startDate, endDate) {
        try {
            const response = await fetch(`${this.baseURL}/api/analytics?start=${startDate}&end=${endDate}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Admin-Secret': this.config.adminSecret
                }
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    success: true,
                    analytics: data
                };
            } else {
                throw new Error(`Analytics fetch failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Analytics fetch error:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    // Check Online Status
    async checkOnlineStatus() {
        try {
            const response = await fetch(`${this.baseURL}/api/status`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.config.key}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                return {
                    online: data.online,
                    agentsAvailable: data.agentsAvailable || 0,
                    averageResponseTime: data.averageResponseTime || '< 1 minute'
                };
            } else {
                throw new Error(`Status check failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Status check error:', error);
            return {
                online: false,
                agentsAvailable: 0,
                averageResponseTime: 'Unknown'
            };
        }
    }

    // End Chat Session
    async endSession() {
        try {
            const response = await fetch(`${this.baseURL}/api/chat/end`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.config.key}`,
                    'X-Session-ID': this.sessionId
                },
                body: JSON.stringify({
                    sessionId: this.sessionId,
                    endTime: new Date().toISOString()
                })
            });

            if (response.ok) {
                this.sessionId = null;
                this.authenticated = false;
                return { success: true };
            } else {
                throw new Error(`Session end failed: ${response.status}`);
            }
        } catch (error) {
            console.error('Session end error:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }
}

// Export for use in the main widget
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ChatAPI;
} else {
    window.ChatAPI = ChatAPI;
}