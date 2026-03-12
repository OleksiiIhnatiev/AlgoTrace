import { reactive } from 'vue';
import api from './api';

// --- Interfaces ---

export interface LoginRequest {
  email: string;
  password: string;
  twoFactorCode?: string | null;
  twoFactorRecoveryCode?: string | null;
}

export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface ResetPasswordRequest {
  email: string;
  resetCode: string;
  newPassword: string;
}

export interface TwoFactorManageRequest {
  enable?: boolean | null;
  twoFactorCode?: string | null;
  resetSharedKey: boolean;
  resetRecoveryCodes: boolean;
  forgetMachine: boolean;
}

export interface ManageInfoRequest {
  newEmail?: string | null;
  newPassword?: string | null;
  oldPassword?: string | null;
}

export interface UserInfo {
  email: string;
  isEmailConfirmed: boolean;
}

// --- Service ---

// Глобальное состояние авторизации
export const authState = reactive({
  user: null as UserInfo | null,
  isAuthenticated: false,
  loading: true // для проверки при старте
});

export const authService = {
  // POST /auth/register
  async register(data: RegisterRequest) {
    return api.post('/auth/register', data);
  },

  // POST /auth/login
  async login(data: LoginRequest) {
    const response = await api.post<LoginResponse>('/auth/login', data);
    if (response.data.accessToken) {
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      // Сразу загружаем данные пользователя после входа
      await this.fetchUserInfo();
    }
    return response.data;
  },

  // Загрузка данных пользователя (email и т.д.)
  async fetchUserInfo() {
    const response = await api.get<UserInfo>('/auth/manage/info');
    authState.user = response.data;
    authState.isAuthenticated = true;
    return response.data;
  },

  // POST /auth/refresh
  // Note: Usually handled automatically by api.ts interceptor, but exposed if needed manually
  async refresh(refreshToken: string) {
    return api.post<LoginResponse>('/auth/refresh', { refreshToken });
  },

  // GET /auth/confirmEmail
  async confirmEmail(userId: string, code: string) {
    return api.get('/auth/confirmEmail', {
      params: { userId, code },
    });
  },

  // POST /auth/resendConfirmationEmail
  async resendConfirmationEmail(email: string) {
    return api.post('/auth/resendConfirmationEmail', { email });
  },

  // POST /auth/forgotPassword
  async forgotPassword(email: string) {
    return api.post('/auth/forgotPassword', { email });
  },

  // POST /auth/resetPassword
  async resetPassword(data: ResetPasswordRequest) {
    return api.post('/auth/resetPassword', data);
  },

  // POST /auth/manage/2fa
  async manage2fa(data: TwoFactorManageRequest) {
    return api.post('/auth/manage/2fa', data);
  },

  // GET /auth/manage/info
  async getUserData() {
    return this.fetchUserInfo();
  },

  // POST /auth/manage/info
  async updateUserInfo(data: ManageInfoRequest) {
    return api.post('/auth/manage/info', data);
  },

  logout() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    authState.user = null;
    authState.isAuthenticated = false;
    window.location.reload(); // Полный сброс состояния
  },

  // Инициализация при старте приложения
  async init() {
    const token = localStorage.getItem('accessToken');
    if (token) {
      try {
        await this.fetchUserInfo();
      } catch { // <-- Просто убираем (error)
        console.warn('Token invalid or expired during init');
        this.logout();
      }
    }
    authState.loading = false;
  },
};
