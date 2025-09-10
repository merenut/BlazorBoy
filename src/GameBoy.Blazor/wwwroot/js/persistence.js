// Browser persistence functions for Game Boy emulator
// Handles localStorage operations for battery-backed RAM and save states

window.gbPersistence = (function() {
    'use strict';

    // Storage key prefix for all Game Boy data
    const STORAGE_PREFIX = 'blazorboy_';
    
    // Check if localStorage is available
    function isLocalStorageAvailable() {
        try {
            const test = '__localStorage_test__';
            localStorage.setItem(test, test);
            localStorage.removeItem(test);
            return true;
        } catch (e) {
            return false;
        }
    }

    // Get storage key with prefix
    function getStorageKey(key) {
        return STORAGE_PREFIX + key;
    }

    // Save battery RAM data to localStorage
    function saveBatteryRam(romKey, batteryData) {
        if (!isLocalStorageAvailable()) {
            console.warn('localStorage not available for battery RAM save');
            return false;
        }

        try {
            const key = getStorageKey(romKey + '_ram');
            if (batteryData && batteryData.length > 0) {
                // Convert byte array to base64 for storage
                const base64Data = btoa(String.fromCharCode(...batteryData));
                localStorage.setItem(key, base64Data);
                console.log(`Saved battery RAM for ${romKey} (${batteryData.length} bytes)`);
            } else {
                // Remove empty data
                localStorage.removeItem(key);
                console.log(`Removed empty battery RAM for ${romKey}`);
            }
            return true;
        } catch (e) {
            console.error('Failed to save battery RAM:', e);
            return false;
        }
    }

    // Load battery RAM data from localStorage
    function loadBatteryRam(romKey) {
        if (!isLocalStorageAvailable()) {
            console.warn('localStorage not available for battery RAM load');
            return null;
        }

        try {
            const key = getStorageKey(romKey + '_ram');
            const base64Data = localStorage.getItem(key);
            
            if (!base64Data) {
                console.log(`No battery RAM found for ${romKey}`);
                return null;
            }

            // Convert base64 back to byte array
            const binaryString = atob(base64Data);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            
            console.log(`Loaded battery RAM for ${romKey} (${bytes.length} bytes)`);
            return bytes;
        } catch (e) {
            console.error('Failed to load battery RAM:', e);
            return null;
        }
    }

    // Save save state data to localStorage
    function saveSaveState(slotKey, saveStateData) {
        if (!isLocalStorageAvailable()) {
            console.warn('localStorage not available for save state');
            return false;
        }

        try {
            const key = getStorageKey('savestate_' + slotKey);
            const base64Data = btoa(String.fromCharCode(...saveStateData));
            localStorage.setItem(key, base64Data);
            console.log(`Saved save state to slot ${slotKey} (${saveStateData.length} bytes)`);
            return true;
        } catch (e) {
            console.error('Failed to save save state:', e);
            return false;
        }
    }

    // Load save state data from localStorage
    function loadSaveState(slotKey) {
        if (!isLocalStorageAvailable()) {
            console.warn('localStorage not available for save state load');
            return null;
        }

        try {
            const key = getStorageKey('savestate_' + slotKey);
            const base64Data = localStorage.getItem(key);
            
            if (!base64Data) {
                console.log(`No save state found for slot ${slotKey}`);
                return null;
            }

            const binaryString = atob(base64Data);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            
            console.log(`Loaded save state from slot ${slotKey} (${bytes.length} bytes)`);
            return bytes;
        } catch (e) {
            console.error('Failed to load save state:', e);
            return null;
        }
    }

    // Get storage usage information
    function getStorageInfo() {
        if (!isLocalStorageAvailable()) {
            return { available: false, used: 0, total: 0 };
        }

        let used = 0;
        for (let key in localStorage) {
            if (key.startsWith(STORAGE_PREFIX)) {
                used += localStorage[key].length;
            }
        }

        // Approximate localStorage limit (varies by browser, typically 5-10MB)
        const total = 5 * 1024 * 1024; // 5MB estimate

        return {
            available: true,
            used: used,
            total: total,
            percentage: Math.round((used / total) * 100)
        };
    }

    // List all stored save data
    function listSaveData() {
        if (!isLocalStorageAvailable()) {
            return [];
        }

        const saveData = [];
        for (let key in localStorage) {
            if (key.startsWith(STORAGE_PREFIX)) {
                const dataType = key.includes('_ram') ? 'battery' : 'savestate';
                const cleanKey = key.replace(STORAGE_PREFIX, '');
                saveData.push({
                    key: cleanKey,
                    type: dataType,
                    size: localStorage[key].length
                });
            }
        }
        return saveData;
    }

    // Clear all Game Boy save data (for debugging/reset purposes)
    function clearAllSaveData() {
        if (!isLocalStorageAvailable()) {
            return false;
        }

        try {
            const keysToRemove = [];
            for (let key in localStorage) {
                if (key.startsWith(STORAGE_PREFIX)) {
                    keysToRemove.push(key);
                }
            }

            keysToRemove.forEach(key => localStorage.removeItem(key));
            console.log(`Cleared ${keysToRemove.length} save data items`);
            return true;
        } catch (e) {
            console.error('Failed to clear save data:', e);
            return false;
        }
    }

    // Export functions
    return {
        saveBatteryRam: saveBatteryRam,
        loadBatteryRam: loadBatteryRam,
        saveSaveState: saveSaveState,
        loadSaveState: loadSaveState,
        getStorageInfo: getStorageInfo,
        listSaveData: listSaveData,
        clearAllSaveData: clearAllSaveData,
        isAvailable: isLocalStorageAvailable
    };
})();