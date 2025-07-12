import { useState, useEffect } from 'react';

/**
 * Custom hook for persisting state in localStorage with the "visiowebtools-" prefix
 * @param key - The key to store the value under (will be prefixed with "visiowebtools-")
 * @param defaultValue - The default value to use if nothing is stored
 * @returns A tuple of [value, setValue] similar to useState
 */
export function useLocalStorage<T>(key: string, defaultValue: T): [T, (value: T) => void] {
  const prefixedKey = `visiowebtools-${key}`;
  
  const [value, setValue] = useState<T>(() => {
    try {
      if (typeof window === 'undefined') {
        return defaultValue;
      }
      const item = window.localStorage.getItem(prefixedKey);
      return item ? JSON.parse(item) : defaultValue;
    } catch (error) {
      console.warn(`Error reading localStorage key "${prefixedKey}":`, error);
      return defaultValue;
    }
  });

  const setStoredValue = (newValue: T) => {
    try {
      setValue(newValue);
      if (typeof window !== 'undefined') {
        window.localStorage.setItem(prefixedKey, JSON.stringify(newValue));
      }
    } catch (error) {
      console.warn(`Error setting localStorage key "${prefixedKey}":`, error);
    }
  };

  return [value, setStoredValue];
}
