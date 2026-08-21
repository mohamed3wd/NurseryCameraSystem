# NurseryCam Parent — Mobile (Capacitor)

The parent Angular app runs on iOS and Android via [Capacitor](https://capacitorjs.com/). The same codebase serves the browser and native shells; Capacitor wraps the built web assets in a WebView.

## Prerequisites

| Tool | Purpose |
|------|---------|
| Node.js 20+ | Build the Angular app |
| Xcode 15+ (macOS) | iOS simulator & device builds |
| CocoaPods (`brew install cocoapods`) | Required for the iOS native project |
| Android Studio | Android emulator & device builds |
| Running API | Backend at `http://localhost:5080` (or your LAN IP) |

## Quick start (development)

1. **Start the backend** (from repo root):

   ```bash
   dotnet run --project src/NurseryCamera.Api
   ```

2. **Set the API host** for your target device in  
   `src/environments/environment.capacitor.dev.ts`:

   | Target | `apiUrl` / `hubUrl` host |
   |--------|--------------------------|
   | iOS Simulator | `http://localhost:5080` |
   | Android Emulator | `http://10.0.2.2:5080` |
   | Physical phone | `http://<your-LAN-IP>:5080` |

3. **Build, sync, and run**:

   ```bash
   cd frontend/parent
   npm install
   npm run cap:run:ios        # or cap:run:android
   ```

   First run creates the `ios/` and `android/` native projects automatically.

   If iOS setup fails with a CocoaPods error, install it then add the platform:

   ```bash
   brew install cocoapods
   npx cap add ios
   npm run cap:sync:dev
   ```

## npm scripts

| Script | Description |
|--------|-------------|
| `npm run build:mobile` | Production mobile build |
| `npm run build:mobile:dev` | Development mobile build (source maps) |
| `npm run cap:sync:dev` | Build dev + copy to native projects + patch HTTP for local API |
| `npm run cap:open:ios` | Open Xcode |
| `npm run cap:open:android` | Open Android Studio |
| `npm run cap:run:ios` | Sync dev build and launch iOS |
| `npm run cap:run:android` | Sync dev build and launch Android |

## Production release

1. Update `src/environments/environment.capacitor.ts` with your HTTPS API URL.
2. Run `npm run cap:sync` (uses the production mobile configuration).
3. Open the native IDE and configure signing:
   - **iOS**: Team + bundle ID `com.nurserycam.parent` in Xcode
   - **Android**: Keystore in Android Studio
4. Remove or restrict the dev-only HTTP patches in `scripts/patch-native-dev.mjs` before store submission.

## What works out of the box

- JWT login / logout (same REST API as web)
- Children list, child detail, camera selection
- WebRTC live view (`playsinline`, background auto-stop for security)
- SignalR real-time session revocation
- English / Arabic with RTL layout
- Safe-area padding for notched phones

## Architecture notes

- **WebView origin**: iOS `capacitor://localhost`, Android `https://localhost`
- **CORS**: Capacitor origins are included in `appsettings.Development.json`
- **Background**: Live viewing stops when the app is backgrounded (battery + security)
- **No RTSP exposure**: Mobile uses the same token + WebRTC signaling path as the browser

## Troubleshooting

**Cannot connect to API on a physical device**  
Use your machine's LAN IP, not `localhost`. Ensure phone and computer are on the same Wi‑Fi and the API listens on `0.0.0.0:5080`.

**Android cleartext HTTP blocked**  
Re-run `npm run cap:sync:dev` — the post-sync script enables dev HTTP. For production, use HTTPS only.

**WebRTC black screen on iOS**  
Confirm the media gateway is reachable from the device and that `playsinline` is set on the `<video>` element (already configured).

**SignalR disconnects**  
Check that `hubUrl` matches `apiUrl` host and that WebSockets are not blocked by a firewall.
