#!/bin/bash
set -e

echo "========================================="
echo "Chatterbox UI Setup Script"
echo "========================================="
echo ""

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed!"
    echo "Please install .NET 10.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK found: $(dotnet --version)"
echo ""

# Initialize git submodules
echo "📦 Initializing git submodules..."
if [ -d ".git" ]; then
    git submodule update --init --recursive
    echo "✅ Submodules initialized"
else
    echo "⚠️  Warning: Not a git repository. Submodule may not be available."
    echo "   If cloning, use: git clone --recurse-submodules <repo-url>"
fi
echo ""

# Setup environment file
if [ ! -f ".env" ]; then
    echo "📝 Creating .env file from template..."
    cp .env.example .env
    echo "✅ .env file created"
    echo ""
    echo "⚠️  IMPORTANT: Edit .env file with your configuration:"
    echo "   - CHATTERBOX_SERVER_URL: URL of your Chatterbox backend (default: http://localhost:20480)"
    echo "   - CHATTERBOX_API_KEY: Your API key for authentication"
    echo ""
    read -p "Press Enter to open .env in nano (or Ctrl+C to skip)..." 
    nano .env 2>/dev/null || vi .env 2>/dev/null || echo "Please edit .env manually"
else
    echo "✅ .env file already exists"
fi
echo ""

# Restore dependencies and build
echo "🔨 Restoring NuGet packages..."
dotnet restore
echo ""

echo "🏗️  Building project..."
dotnet build --configuration Release
echo ""

echo "========================================="
echo "✅ Setup Complete!"
echo "========================================="
echo ""
echo "To run the application:"
echo "  Development:  dotnet run"
echo "  Production:   dotnet run --configuration Release"
echo ""
echo "The application will be available at:"
echo "  http://localhost:5000"
echo "  https://localhost:5001"
echo ""
echo "To run in background (systemd recommended):"
echo "  nohup dotnet run --configuration Release &"
echo ""
echo "For production deployment, consider:"
echo "  1. Publishing: dotnet publish -c Release -o ./publish"
echo "  2. Setting up systemd service"
echo "  3. Configuring reverse proxy (nginx/caddy)"
echo ""
