# Chatterbox UI

Blazor Server web interface for [Chatterbox Inference](https://github.com/solemn-solitude/chatterbox-inference-thing) TTS engine.

## Features

- 🎙️ **Voice Management** - List, upload, and delete voice references
- 🗣️ **Text-to-Speech** - Synthesize speech with default or cloned voices
- 🎵 **Audio Playback** - Real-time browser-based playback
- 🎤 **Microphone Recording** - Record voice samples directly in the browser
- 🔒 **Secure Configuration** - Environment-based secrets management

## Prerequisites

- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Git** - For cloning with submodules
- **Chatterbox Backend** - Running inference server

## Quick Start (Linux)

```bash
# Clone with submodules
git clone --recurse-submodules https://github.com/yourusername/chatterbox-ui
cd chatterbox-ui

# Run setup script
chmod +x setup.sh
./setup.sh

# Edit .env with your configuration
nano .env

# Run the application
dotnet run
```

Visit http://localhost:5000/chatterbox

## Manual Setup

### 1. Clone Repository

```bash
git clone https://github.com/yourusername/chatterbox-ui
cd chatterbox-ui
```

### 2. Initialize Submodules

```bash
git submodule update --init --recursive
```

### 3. Configure Environment

```bash
cp .env.example .env
nano .env
```

Edit `.env`:
```env
CHATTERBOX_SERVER_URL=http://localhost:20480
CHATTERBOX_API_KEY=your-api-key-here
```

### 4. Build & Run

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run (Development)
dotnet run

# Run (Production)
dotnet run --configuration Release
```

## Production Deployment

### Option 1: Publish & Run

```bash
# Publish optimized build
dotnet publish -c Release -o ./publish

# Run published app
cd publish
./chatterbox-ui
```

### Option 2: Systemd Service (Recommended)

Create `/etc/systemd/system/chatterbox-ui.service`:

```ini
[Unit]
Description=Chatterbox UI Web Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/chatterbox-ui
ExecStart=/usr/bin/dotnet /opt/chatterbox-ui/chatterbox-ui.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=chatterbox-ui
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl enable chatterbox-ui
sudo systemctl start chatterbox-ui
sudo systemctl status chatterbox-ui
```

### Option 3: Reverse Proxy (Nginx)

`/etc/nginx/sites-available/chatterbox-ui`:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable and reload:
```bash
sudo ln -s /etc/nginx/sites-available/chatterbox-ui /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `CHATTERBOX_SERVER_URL` | Backend TTS server URL | `http://localhost:20480` |
| `CHATTERBOX_API_KEY` | Authentication API key | (required) |

### Application Settings

Edit `appsettings.json` for logging and other ASP.NET Core settings.

## Project Structure

```
chatterbox-ui/
├── lib/                                    # Git submodules
│   └── chatterbox-inference-client-csharp-bindings/
├── Components/
│   ├── Pages/
│   │   ├── Chatterbox.razor              # Main TTS page
│   │   ├── Home.razor                    # Landing page
│   │   └── VoiceListItem.razor           # Voice list component
│   └── Layout/
│       ├── MainLayout.razor              # App layout
│       └── NavMenu.razor                 # Navigation
├── Services/
│   ├── ChatterboxService.cs              # TTS client wrapper
│   └── ChatterboxConfig.cs               # Configuration model
├── wwwroot/
│   └── chatterbox.js                     # Audio recording/playback
├── .env.example                          # Environment template
├── .gitignore                            # Git ignore rules
├── Program.cs                            # App entry point
├── setup.sh                              # Linux setup script
└── README.md                             # This file
```

## Development

### Running Locally

```bash
dotnet watch run
```

The app will auto-reload on file changes.

### Adding Features

1. Edit Razor components in `Components/Pages/`
2. Modify services in `Services/`
3. Update JavaScript in `wwwroot/chatterbox.js`

## Troubleshooting

### Submodule Not Found

```bash
git submodule update --init --recursive
```

### Build Errors

Clear build artifacts:
```bash
dotnet clean
rm -rf bin/ obj/
dotnet restore
dotnet build
```

### Port Already in Use

Change port in `Properties/launchSettings.json` or use:
```bash
dotnet run --urls="http://localhost:5001"
```

### .env Not Loading

Ensure `DotNetEnv` package is installed:
```bash
dotnet add package DotNetEnv
```

## License

Same as parent project.

## Links

- [Chatterbox Inference Backend](https://github.com/solemn-solitude/chatterbox-inference-thing)
- [C# Client Library](https://github.com/solemn-solitude/chatterbox-inference-client-csharp-bindings)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
