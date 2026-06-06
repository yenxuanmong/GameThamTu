// ============================================
// Auth Routes — /api/auth
// ============================================

import { Router } from 'express';
import {
  register,
  login,
  logout,
  refreshToken,
  getProfile,
  updateProfile,
  changePassword,
  registerValidation,
  loginValidation,
} from '../controllers/authController';
import {
  forgotPassword,
  verifyResetToken,
  resetPassword,
  forgotPasswordValidation,
  resetPasswordValidation,
} from '../controllers/passwordResetController';
import { uploadAvatar, deleteAvatar } from '../controllers/uploadController';
import { requireAuth } from '../middleware/authMiddleware';
import { authRateLimiter as rateLimiter } from '../middleware/rateLimiter';
import { avatarUpload } from '../middleware/upload';

const router = Router();

// ---- Public ----
router.post('/register', rateLimiter, registerValidation, register);
router.post('/login', rateLimiter, loginValidation, login);
router.post('/refresh', refreshToken);

// ---- Password reset ----
router.post('/forgot-password', rateLimiter, forgotPasswordValidation, forgotPassword);
router.get('/reset-password/verify', verifyResetToken);
router.post('/reset-password', rateLimiter, resetPasswordValidation, resetPassword);

// ---- Protected ----
router.get('/me', requireAuth, getProfile);
router.patch('/me', requireAuth, updateProfile);
router.post('/me/change-password', requireAuth, changePassword);
router.post('/logout', requireAuth, logout);

// ---- Avatar ----
router.post('/me/avatar', requireAuth, avatarUpload.single('avatar'), uploadAvatar);
router.delete('/me/avatar', requireAuth, deleteAvatar);

export default router;
