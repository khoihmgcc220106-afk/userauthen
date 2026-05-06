<script setup>
import { ref, reactive } from 'vue';
import axios from 'axios';

// Lấy URL từ biến môi trường của Vercel, nếu không có thì dùng link mặc định
const API_BASE_URL = import.meta.env.VITE_API_URL || "https://userauthen.onrender.com";
const LOGIN_URL = `${API_BASE_URL}/api/users/login`;

const loginForm = reactive({
  username: '',
  password: '' // Đổi từ passwordHash thành password để khớp với DTO của C#
});

const loading = ref(false);
const message = ref('');
const statusClass = ref('');

const handleLogin = async () => {
  loading.value = true;
  message.value = '';
  
  try {
    // Gửi đúng object mà Backend mong đợi
    const response = await axios.post(LOGIN_URL, {
        username: loginForm.username,
        password: loginForm.password
    });
    
    const token = response.data.token;
    localStorage.setItem('userToken', token);
    
    message.value = "Đăng nhập thành công!";
    statusClass.value = "success";
  } catch (error) {
    statusClass.value = "error";
    // Nếu lỗi 500 hoặc 401, hiển thị chi tiết từ Backend trả về
    message.value = error.response?.data?.message || "Lỗi kết nối Server!";
  } finally {
    loading.value = false;
  }
};
</script>