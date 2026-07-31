import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: "/login/",
  server: {
    port: 8001,
    strictPort: true,
    host: true,
  },
});
