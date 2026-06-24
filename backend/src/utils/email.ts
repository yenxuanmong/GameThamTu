// ============================================
// Email Utility — password reset emails
// ============================================

import nodemailer from 'nodemailer';
import logger from './logger';

function createTransporter() {
  const host = process.env['SMTP_HOST'];
  const port = parseInt(process.env['SMTP_PORT'] ?? '587', 10);
  const user = process.env['SMTP_USER'];
  const pass = process.env['SMTP_PASS'];

  // In development, use Ethereal (fake SMTP) or console
  if (!host || !user) {
    if (process.env['NODE_ENV'] !== 'production') {
      // Fallback: log to console in dev
      return null;
    }
    throw new Error('SMTP_HOST and SMTP_USER must be set in production');
  }

  return nodemailer.createTransport({
    host,
    port,
    secure: port === 465,
    auth: { user, pass },
  });
}

// ============================================
// Send password reset email
// ============================================

export async function sendPasswordResetEmail(
  toEmail: string,
  username: string,
  resetToken: string,
  resetUrl: string
): Promise<void> {
  const transporter = createTransporter();
  const appName = process.env['APP_NAME'] ?? 'Detective Royale';
  const expiryMinutes = 30;

  const html = `
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"></head>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #1a1a2e;">
  <div style="background: #16213e; padding: 30px; border-radius: 8px; text-align: center; margin-bottom: 30px;">
    <h1 style="color: #e94560; margin: 0; font-size: 24px;">🔍 ${appName}</h1>
    <p style="color: #a0aec0; margin: 8px 0 0;">Password Reset Request</p>
  </div>

  <p>Hello <strong>${username}</strong>,</p>
  <p>We received a request to reset your password. Click the button below to proceed:</p>

  <div style="text-align: center; margin: 30px 0;">
    <a href="${resetUrl}" 
       style="background: #e94560; color: white; padding: 14px 32px; border-radius: 6px; 
              text-decoration: none; font-weight: bold; font-size: 16px; display: inline-block;">
      Reset My Password
    </a>
  </div>

  <p style="color: #718096; font-size: 14px;">
    This link expires in <strong>${expiryMinutes} minutes</strong>.<br>
    If you did not request a password reset, you can safely ignore this email.
  </p>

  <hr style="border: none; border-top: 1px solid #2d3748; margin: 20px 0;">
  <p style="color: #718096; font-size: 12px; text-align: center;">
    If the button doesn't work, copy this link:<br>
    <a href="${resetUrl}" style="color: #e94560;">${resetUrl}</a>
  </p>
</body>
</html>`;

  const text = `Hello ${username},\n\nReset your password here: ${resetUrl}\n\nThis link expires in ${expiryMinutes} minutes.\n\nIf you didn't request this, ignore this email.`;

  if (!transporter) {
    // Dev mode: log to console
    logger.info('[Email] Password reset (dev — not sent)', {
      to: toEmail,
      resetUrl,
      token: resetToken,
    });
    return;
  }

  await transporter.sendMail({
    from: `"${appName}" <${process.env['SMTP_FROM'] ?? process.env['SMTP_USER']}>`,
    to: toEmail,
    subject: `Reset your ${appName} password`,
    html,
    text,
  });

  logger.info('[Email] Password reset email sent', { to: toEmail });
}
