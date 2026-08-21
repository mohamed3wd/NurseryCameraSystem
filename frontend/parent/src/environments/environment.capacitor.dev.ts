/**
 * Local mobile development against a machine-running API.
 *
 * Pick the host that matches your target:
 * - iOS Simulator:       http://localhost:5080
 * - Android Emulator:    http://10.0.2.2:5080
 * - Physical device:     http://<your-LAN-IP>:5080  (e.g. http://192.168.1.42:5080)
 *
 * Update the URLs below before running `npm run cap:run:*`.
 */
export const environment = {
  production: false,
  apiUrl: 'http://10.0.2.2:5080/api',
  hubUrl: 'http://10.0.2.2:5080/hubs/nursery'
};
