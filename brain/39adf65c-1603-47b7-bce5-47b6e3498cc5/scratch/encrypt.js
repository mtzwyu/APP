const crypto = require('crypto');

const secret = "OlapAnalyticsDefaultSecretKey2024!";
const ivStr = "OlapAnalyticsIV!!";

const key = crypto.createHash('sha256').update(secret).digest();
const iv = crypto.createHash('md5').update(ivStr).digest();

function encrypt(text) {
    let cipher = crypto.createCipheriv('aes-256-cbc', key, iv);
    let encrypted = cipher.update(text, 'utf8', 'base64');
    encrypted += cipher.final('base64');
    return encrypted;
}

const targets = [
    "OlapAnalyticsSuperSecretKey2024!!Secure",
    "Server=localhost\\MANHTRUONG1;Database=AppAnalytics;User Id=sa;Password=123;TrustServerCertificate=True;",
    "Server=localhost\\MANHTRUONG1;Database=master;User Id=sa;Password=123;TrustServerCertificate=True;"
];

targets.forEach(t => {
    console.log(`Target: ${t}`);
    console.log(`Encrypted: ${encrypt(t)}`);
    console.log('---');
});
