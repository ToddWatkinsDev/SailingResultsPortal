// Cache Manager for Sailing Results Portal
// Handles timestamp-based caching to reduce server load

class CacheManager {
    constructor() {
        this.checkInterval = 30000; // Check every 30 seconds
        this.currentTimestamps = {};
        this.isChecking = false;
    }

    // Initialize cache checking for a page
    init(pageType, eventId = null) {
        console.log(`Initializing cache manager for ${pageType}`, eventId ? `event: ${eventId}` : '');

        // Store initial timestamps from server
        if (typeof window.eventsTimestamp !== 'undefined') {
            this.currentTimestamps.events = window.eventsTimestamp;
        }
        if (typeof window.resultsTimestamp !== 'undefined') {
            this.currentTimestamps.results = window.resultsTimestamp;
        }

        console.log('Initial timestamps:', this.currentTimestamps);

        // Start periodic checking
        this.startChecking(pageType, eventId);
    }

    // Start periodic timestamp checking
    startChecking(pageType, eventId) {
        setInterval(() => {
            if (!this.isChecking) {
                this.checkForUpdates(pageType, eventId);
            }
        }, this.checkInterval);
    }

    // Check if data has been updated
    async checkForUpdates(pageType, eventId) {
        this.isChecking = true;

        try {
            const response = await fetch(`/api/cache/timestamps?eventId=${eventId || ''}`);
            const newTimestamps = await response.json();

            console.log('Checking for updates...', newTimestamps);

            let needsRefresh = false;

            // Check if any timestamps have changed
            if (this.currentTimestamps.events !== newTimestamps.events) {
                console.log('Events data has been updated');
                needsRefresh = true;
            }

            if (this.currentTimestamps.results !== newTimestamps.results) {
                console.log('Results data has been updated');
                needsRefresh = true;
            }

            if (needsRefresh) {
                console.log('Data has changed, refreshing page...');
                this.showUpdateNotification();
                setTimeout(() => {
                    window.location.reload();
                }, 2000); // Give user time to see notification
            } else {
                console.log('Data is up to date');
            }

            // Update current timestamps
            this.currentTimestamps = newTimestamps;

        } catch (error) {
            console.error('Error checking for updates:', error);
        } finally {
            this.isChecking = false;
        }
    }

    // Show notification that data has been updated
    showUpdateNotification() {
        // Remove any existing notification
        const existingNotification = document.getElementById('cache-update-notification');
        if (existingNotification) {
            existingNotification.remove();
        }

        // Create notification element
        const notification = document.createElement('div');
        notification.id = 'cache-update-notification';
        notification.className = 'fixed top-4 right-4 bg-blue-600 text-white px-6 py-3 rounded-lg shadow-lg z-50';
        notification.innerHTML = `
            <div class="flex items-center">
                <i class="fas fa-sync-alt fa-spin mr-2"></i>
                <span>Data has been updated. Refreshing...</span>
            </div>
        `;

        document.body.appendChild(notification);

        // Auto-remove after animation
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 2500);
    }

    // Manual refresh trigger
    refreshNow() {
        console.log('Manual refresh triggered');
        window.location.reload();
    }
}

// Global cache manager instance
window.cacheManager = new CacheManager();

// Auto-initialize based on current page
document.addEventListener('DOMContentLoaded', function() {
    const bodyClass = document.body.className || '';
    const urlParams = new URLSearchParams(window.location.search);
    const eventId = urlParams.get('eventId');

    if (bodyClass.includes('results-index') || window.location.pathname.includes('/Results')) {
        window.cacheManager.init('results', eventId);
    } else if (bodyClass.includes('events-index') || window.location.pathname.includes('/Events')) {
        window.cacheManager.init('events', eventId);
    }
});