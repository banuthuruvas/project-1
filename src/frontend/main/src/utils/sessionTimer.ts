import { ref } from "vue";

let apiActivityTimeout: ReturnType<typeof setTimeout> | null = null;
export const showPopup = ref(false);
let currentTimeoutId = 0;

export const resetSessionTimer = () => {
  if (apiActivityTimeout) {
    clearTimeout(apiActivityTimeout);
  }
  const timeoutId = ++currentTimeoutId;
  apiActivityTimeout = setTimeout(() => {
    if (timeoutId === currentTimeoutId) {
      showPopup.value = true;
    }
  }, import.meta.env.VITE_SESSION_TIMEOUT_MINS * 60 * 1000);
};

export const getShowPopup = () => showPopup;
