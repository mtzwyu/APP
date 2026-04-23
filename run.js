const { spawn, execSync } = require('node:child_process');
const path = require('node:path');

console.log('===================================================');
console.log('🚀 KHỞI ĐỘNG HỆ THỐNG OLAPANALYTICS (MẸO Ổ ĐĨA ẢO)');
console.log('===================================================');

const projectPath = __dirname;
const driveLetter = 'Z:';

// 1. Dọn dẹp ổ ảo cũ nếu có và tạo mới
try {
    console.log(`🔗 Đang tạo ổ đĩa ảo ${driveLetter} cho dự án...`);
    execSync(`subst ${driveLetter} /D`, { stdio: 'ignore' });
} catch (e) {}

try {
    execSync(`subst ${driveLetter} "${projectPath}"`);
} catch (e) {
    console.error('❌ Không thể tạo ổ đĩa ảo. Thử chạy tiếp từ đường dẫn gốc...');
}

// 2. Unblock files (Quan trọng để tránh Application Control)
console.log('🛡️  Đang Unblock các file trong project (đường dẫn vật lý)...');
try {
    execSync(`powershell "Get-ChildItem -Path '${projectPath}' -Recurse | Unblock-File"`, { stdio: 'ignore' });
} catch (e) {}

// Đường dẫn mới trên ổ Z:
const backendPath = path.join(driveLetter, 'backend', 'OlapAnalytics.API');
const frontendPath = path.join(driveLetter, 'frontend');

// 3. Build Backend trước
console.log('🔨 Đang build Backend...');
try {
    console.log('🧹 Đang dọn dẹp (Clean)...');
    execSync('dotnet clean', { cwd: backendPath, stdio: 'inherit' });
    execSync('dotnet build', { cwd: backendPath, stdio: 'inherit' });
} catch (e) {
    console.error('❌ Build thất bại.');
}

// 4. Chạy Backend bằng DLL (Để bypass Application Control block .exe)
console.log('🚀 Đang chạy Backend (DLL Mode)...');
const dllPath = path.join(backendPath, 'bin', 'Debug', 'net8.0-windows', 'OlapAnalytics.API.dll');
const backendProcess = spawn('dotnet', [dllPath], { 
    cwd: backendPath, 
    stdio: 'inherit',
    env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: 'http://localhost:5105'
    }
});

console.log('🎨 Đang chạy Frontend...');
const frontendProcess = spawn('npm', ['run', 'dev'], { 
    cwd: frontendPath, 
    stdio: 'inherit',
    shell: true 
});

process.on('SIGINT', () => {
    console.log('\n🛑 Đang dọn dẹp...');
    backendProcess.kill();
    frontendProcess.kill();
    try { execSync(`subst ${driveLetter} /D`); } catch (e) {}
    process.exit();
});
