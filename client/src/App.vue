<template>
  <div class="login-container">
    <h2>Hệ Thống Đăng Nhập</h2>
    <form @submit.prevent="handleLogin">
      <div class="form-group">
        <label>Tài khoản:</label>
        <input v-model="loginForm.username" type="text" required placeholder="Nhập username" />
      </div>

      <div class="form-group">
        <label>Mật khẩu:</label>
        <input v-model="loginForm.password" type="password" required placeholder="Nhập password" />
      </div>

      <button type="submit" :disabled="loading">
        {{ loading ? 'Đang xử lý...' : 'Đăng Nhập' }}
      </button>
    </form>

    <p v-if="message" :class="statusClass" class="status-msg">{{ message }}</p>
    
    <div v-if="token" class="token-box">
      <p><strong>Token của bạn:</strong></p>
      <textarea readonly :value="token"></textarea>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || "https://userauthen.onrender.com";
const LOGIN_URL = `${API_BASE_URL}/api/users/login`;

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
    // Backend dùng model User nên cần gửi đúng tên trường là passwordHash
    const response = await axios.post(LOGIN_URL, {
        username: loginForm.username,
        passwordHash: loginForm.password // Gửi pass vào trường passwordHash theo ý Backend
    });
    
    token.value = response.data.token;
    localStorage.setItem('userToken', token.value);
    
    message.value = "Đăng nhập thành công! Chào " + response.data.username;
    statusClass.value = "success";
  } catch (error) {
    statusClass.value = "error";
    message.value = error.response?.data?.message || "Sai tài khoản hoặc mật khẩu!";
    console.error(error);
  } finally {
    loading.value = false;
  }
};
</script>

<style>
/* Style này để đảm bảo không bị trắng trang */
body { background-color: #f0f2f5; font-family: sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; }
.login-container { background: white; padding: 2rem; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); width: 100%; max-width: 400px; color: #333; }
h2 { text-align: center; color: #2c3e50; }
.form-group { margin-bottom: 1rem; }
label { display: block; margin-bottom: 5px; font-weight: bold; }
input { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }
button { width: 100%; padding: 10px; background: #42b983; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; transition: 0.3s; }
button:hover { background: #3aa876; }
button:disabled { background: #94d3b4; }
.status-msg { text-align: center; margin-top: 1rem; font-weight: bold; }
.success { color: #2ecc71; }
.error { color: #e74c3c; }
.token-box { margin-top: 1rem; font-size: 12px; }
textarea { width: 100%; height: 60px; margin-top: 5px; }
</style>