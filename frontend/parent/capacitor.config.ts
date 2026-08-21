import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.nurserycam.parent',
  appName: 'NurseryCam',
  webDir: 'dist/parent/browser',
  server: {
    // Android WebView origin is https://localhost; iOS uses capacitor://localhost.
    androidScheme: 'https',
    // Required for HTTP API calls during local development (replace with HTTPS in production).
    cleartext: true
  },
  android: {
    allowMixedContent: true,
    webContentsDebuggingEnabled: true
  },
  ios: {
    contentInset: 'automatic',
    allowsLinkPreview: false
  },
  plugins: {
    SplashScreen: {
      launchShowDuration: 1500,
      launchAutoHide: true,
      backgroundColor: '#faf7f2',
      showSpinner: false
    },
    Keyboard: {
      resize: 'body',
      resizeOnFullScreen: true
    }
  }
};

export default config;
