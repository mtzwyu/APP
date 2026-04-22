const { spawn } = require('node:child_process');
const path = require('node:path');

console.log('===================================================');
console.log('🚀 KHỞI ĐỘNG HỆ THỐNG OLAPANALYTICS (NODE JS)');
console.log('===================================================');

const backendPath = path.join(__dirname, 'backend', 'OlapAnalytics.API');
const frontendPath = path.join(__dirname, 'frontend');

// Chạy thư mục Backend
const backendProcess = spawn('dotnet', ['run'], { 
    cwd: backendPath, 
    stdio: 'inherit', // Gộp trực tiếp log của backend vào cửa sổ này
    shell: true 
});

// Chạy thư mục Frontend
const frontendProcess = spawn('npm', ['run', 'dev'], { 
    cwd: frontendPath, 
    stdio: 'inherit', // Gộp trực tiếp log của react vào cửa sổ này
    shell: true 
});

backendProcess.on('error', (err) => console.error('Lỗi khi chạy Backend:', err));
frontendProcess.on('error', (err) => console.error('Lỗi khi chạy Frontend:', err));

// Xử lý khi người dùng ấn Ctrl+C để tắt
process.on('SIGINT', () => {
    console.log('\nĐang ngắt kết nối hệ thống...');
    backendProcess.kill('SIGINT');
    frontendProcess.kill('SIGINT');
    process.exit();
});
