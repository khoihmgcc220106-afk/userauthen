<template>
  <div class="login-wrapper">
    <div class="login-card">
      <h2>Hệ Thống Xác Thực</h2>
      <p class="subtitle">Đăng nhập để tiếp tục</p>

      <form @submit.prevent="handleLogin">
        <div class="input-group">
          <label>Tên đăng nhập</label>
          <input 
            v-model="loginForm.username" 
            type="text" 
            required 
            placeholder="Ví dụ: user1" 
          />
        </div>

        <div class="input-group">
          <label>Mật khẩu</label>
          <input 
            v-model="loginForm.password" 
            type="password" 
            required 
            placeholder="••••••••" 
          />
        </div>

        <button type="submit" :disabled="loading" class="login-btn">
          <span v-if="loading">Đang xác thực...</span>
          <span v-else>Đăng Nhập</span>
        </button>
      </form>

      <div v-if="message" :class="['status-box', statusClass]">
        {{ message }}
      </div>

      <div v-if="token" class="token-result">
        <p><strong>JWT Token:</strong></p>
        <code>{{ token.substring(0, 30) }}...</code>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import axios from 'axios';

// Lấy URL từ biến môi trường Vercel hoặc Render
const API_URL = import.meta.env.VITE_API_URL || "https://userauthen.onrender.com";

const loginForm = reactive({
  username: '',
  password: ''
});

const loading = ref(false);
const message = ref('');
const statusClass = ref('');
const token = ref('');

const handleLogin = async () => {
  loading.value = true;
  message.value = '';
  token.value = '';
  
  try {
    // KHỚP CHUẨN BACKEND: 
    // Backend dùng [FromBody] User loginInfo -> cần gửi 'passwordHash'
    const payload = {
      id: 0,
      username: loginForm.username,
      passwordHash: loginForm.password // Gửi pass vào key 'passwordHash'
    };

    const response = await axios.post(`${API_URL}/api/users/login`, payload);
    
    // Lưu kết quả
    token.value = response.data.token;
    localStorage.setItem('userToken', token.value);
    
    message.value = "✅ Đăng nhập thành công!";
    statusClass.value = "success";
  } catch (error) {
    statusClass.value = "error";
    if (error.response) {
      // Backend trả về: Unauthorized(new { message = "..." })
      message.value = "❌ " + (error.response.data.message || "Sai tài khoản/mật khẩu");
    } else {
      message.value = "❌ Không thể kết nối tới Server Render!";
    }
  } finally {
    loading.value = false;
  }
};
</script>

<style>
/* Reset và Style tổng thể để tránh trắng trang */
body {
  margin: 0;
  padding: 0;
  background: #f4f7f9;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.login-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
}

.login-card {
  background: #ffffff;
  padding: 40px;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0,0,0,0.1);
  width: 100%;
  max-width: 360px;
  text-align: center;
}

h2 { margin: 0; color: #333; }
.subtitle { color: #666; margin-bottom: 30px; font-size: 14px; }

.input-group { text-align: left; margin-bottom: 20px; }
.input-group label { display: block; font-size: 13px; font-weight: 600; color: #555; margin-bottom: 8px; }
.input-group input {
  width: 100%;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 6px;
  box-sizing: border-box;
  font-size: 15px;
}

.login-btn {
  width: 100%;
  padding: 12px;
  background: #42b983;
  color: white;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  font-weight: bold;
  cursor: pointer;
  transition: background 0.3s;
}
.login-btn:hover { background: #3aa876; }
.login-btn:disabled { background: #a8dcc3; cursor: not-allowed; }

.status-box {
  margin-top: 20px;
  padding: 10px;
  border-radius: 6px;
  font-size: 14px;
}
.success { background: #e8f5e9; color: #2e7d32; border: 1px solid #c8e6c9; }
.error { background: #ffebee; color: #c62828; border: 1px solid #ffcdd2; }

.token-result { margin-top: 20px; font-size: 11px; color: #888; overflow-wrap: break-word; }
code { background: #eee; padding: 2px 4px; }
</style>