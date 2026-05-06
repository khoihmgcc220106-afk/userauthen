<template>
  <div class="login-container">
    <h2>Đăng Nhập</h2>
    <form @submit.prevent="handleLogin">
      <div class="form-group">
        <label>Tài khoản:</label>
        <input v-model="loginForm.username" type="text" required placeholder="Nhập username" />
      </div>

      <div class="form-group">
        <label>Mật khẩu:</label>
        <input v-model="loginForm.passwordHash" type="password" required placeholder="Nhập password" />
      </div>

      <button type="submit" :disabled="loading">
        {{ loading ? 'Đang xử lý...' : 'Đăng Nhập' }}
      </button>
    </form>

    <p v-if="message" :class="statusClass">{{ message }}</p>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import axios from 'axios';

// 1. Khai báo URL Backend (Thay link Render của bạn vào đây)
const API_URL = "https://userauthen.onrender.com/api/users/login";

const loginForm = reactive({
  username: '',
  passwordHash: '' // Phải khớp với tên thuộc tính trong file User Model C#
});

const loading = ref(false);
const message = ref('');
const statusClass = ref('');

const handleLogin = async () => {
  loading.value = true;
  message.value = '';
  
  try {
    const response = await axios.post(API_URL, loginForm);
    
    // Lưu Token vào LocalStorage để dùng cho các request sau
    const token = response.data.token;
    localStorage.setItem('userToken', token);
    
    message.value = "Đăng nhập thành công! Chào " + response.data.username;
    statusClass.value = "success";
    
    console.log("Token của bạn:", token);
  } catch (error) {
    statusClass.value = "error";
    if (error.response) {
      message.value = error.response.data.message || "Sai tài khoản hoặc mật khẩu!";
    } else {
      message.value = "Không thể kết nối đến Server!";
    }
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.login-container { max-width: 300px; margin: 50px auto; padding: 20px; border: 1px solid #ccc; border-radius: 8px; }
.form-group { margin-bottom: 15px; }
input { width: 100%; padding: 8px; margin-top: 5px; box-sizing: border-box; }
button { width: 100%; padding: 10px; background-color: #42b983; color: white; border: none; cursor: pointer; }
button:disabled { background-color: #ccc; }
.success { color: green; margin-top: 10px; }
.error { color: red; margin-top: 10px; }
</style>