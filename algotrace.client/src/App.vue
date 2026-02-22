<script setup lang="ts">
import { ref } from 'vue';
import AnalyzerPage from './components/AnalyzerPage.vue'; // Підключаємо сторінку аналізатора

const isAnalyzerOpen = ref(false); // Змінна для перемикання екранів

const level = ref(1);
const file1 = ref<File | null>(null);
const file2 = ref<File | null>(null);
const isDragging1 = ref(false);
const isDragging2 = ref(false);

const handleDrop = (event: DragEvent, fileNumber: 1 | 2) => {
  const files = event.dataTransfer?.files;
  if (files && files.length > 0) {
    validateAndSetFile(files[0]!, fileNumber);
  }
  if (fileNumber === 1) isDragging1.value = false;
  else isDragging2.value = false;
};

const handleFileSelect = (event: Event, fileNumber: 1 | 2) => {
  const input = event.target as HTMLInputElement;
  if (input.files && input.files.length > 0) {
    validateAndSetFile(input.files[0]!, fileNumber);
  }
};

const validateAndSetFile = (file: File, fileNumber: 1 | 2) => {
  const allowedExtensions = ['.cs', '.py', '.js', '.ts', '.java', '.cpp', '.h', '.html', '.css', '.php', '.rb', '.go'];
  const fileName = file.name.toLowerCase();
  const isValid = allowedExtensions.some(ext => fileName.endsWith(ext));

  if (isValid) {
    if (fileNumber === 1) file1.value = file;
    else file2.value = file;
  } else {
    alert('Будь ласка, виберіть файл із кодом (наприклад: .cs, .py, .js)');
  }
};

const removeFile = (fileNumber: 1 | 2) => {
  if (fileNumber === 1) file1.value = null;
  else file2.value = null;
};

const startComparison = () => {
  if (!file1.value || !file2.value) {
    alert('Будь ласка, завантажте обидва файли для порівняння.');
    return;
  }
  
  // Замість alert тепер просто перемикаємо екран на аналізатор
  isAnalyzerOpen.value = true; 
};
</script>

<template>
  <div class="main-wrapper">
    <transition name="fade" mode="out-in">
      
      <div v-if="!isAnalyzerOpen" class="min-vh-100 bg-body-tertiary" key="landing">
        <nav class="navbar navbar-expand-lg navbar-light bg-white sticky-top shadow-sm">
          <div class="container">
            <a class="navbar-brand fw-bold d-flex align-items-center text-primary" href="#">
              <div class="bg-primary bg-gradient text-white rounded-3 p-2 me-2 d-flex align-items-center justify-content-center shadow-sm" style="width: 40px; height: 40px;">
                <i class="bi bi-layers-half fs-5"></i>
              </div>
              <span>AlgoTrace</span>
            </a>

            <div class="d-flex align-items-center">
              <div class="dropdown">
                <a href="#" class="d-flex align-items-center text-dark text-decoration-none dropdown-toggle" id="dropdownUser1" data-bs-toggle="dropdown" aria-expanded="false">
                  <i class="bi bi-person-circle fs-4 me-2 text-secondary"></i>
                  <span class="fw-medium">Увійти</span>
                </a>
              </div>
            </div>
          </div>
        </nav>

        <div class="container py-5">
          <div class="text-center mb-5">
            <h1 class="display-4 fw-bolder text-dark mb-3">
              Порівняння коду <span class="text-primary">нового рівня</span>
            </h1>
            <p class="lead text-secondary mx-auto col-lg-7">
              Завантажте файли, налаштуйте глибину аналізу та отримайте миттєвий результат.
            </p>
          </div>

          <div class="row g-4 justify-content-center align-items-stretch">
            <div class="col-md-5">
              <div
                class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden position-relative"
                :class="isDragging1 ? 'bg-primary bg-opacity-10 border border-2 border-primary' : 'bg-white'"
                @dragover.prevent="isDragging1 = true"
                @dragleave.prevent="isDragging1 = false"
                @drop.prevent="(e) => handleDrop(e, 1)"
              >
                <div class="card-body d-flex flex-column align-items-center justify-content-center p-5 text-center" style="min-height: 350px;">
                  <div v-if="!file1">
                    <div class="mb-4 p-4 rounded-circle bg-primary bg-opacity-10 text-primary d-inline-flex shadow-sm">
                      <i class="bi bi-cloud-arrow-up-fill display-4"></i>
                    </div>
                    <h4 class="fw-bold mb-2 text-dark">Файл 1</h4>
                    <p class="text-muted mb-4 small">Перетягніть файл сюди або натисніть кнопку</p>
                    <label class="btn btn-outline-primary rounded-pill px-4 py-2 fw-bold border-2">
                      <i class="bi bi-folder-plus me-2"></i> Обрати файл
                      <input type="file" hidden @change="(e) => handleFileSelect(e, 1)" accept=".cs,.py,.js,.ts,.java,.cpp,.h,.html,.css,.php,.rb,.go" />
                    </label>
                  </div>
                  <div v-else>
                    <div class="mb-3 text-success position-relative">
                      <i class="bi bi-file-earmark-check-fill display-1"></i>
                      <span class="position-absolute top-0 start-100 translate-middle p-2 bg-success border border-light rounded-circle"></span>
                    </div>
                    <h5 class="fw-bold text-break mb-1 text-dark">{{ file1.name }}</h5>
                    <span class="badge bg-light text-secondary border mb-4 px-3 py-2 rounded-pill">{{ (file1.size / 1024).toFixed(2) }} KB</span>
                    <div>
                      <button class="btn btn-light text-danger btn-sm rounded-pill px-4 py-2 fw-bold shadow-sm" @click="removeFile(1)">
                        <i class="bi bi-trash3-fill me-1"></i> Видалити
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="col-md-auto d-flex align-items-center justify-content-center py-3 py-md-0">
              <div class="bg-white rounded-circle shadow-lg p-3 text-primary fw-bold fs-4 d-flex align-items-center justify-content-center border border-4 border-light" style="width: 70px; height: 70px; z-index: 10;">
                <i class="bi bi-arrow-left-right"></i>
              </div>
            </div>

            <div class="col-md-5">
              <div
                class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden position-relative"
                :class="isDragging2 ? 'bg-primary bg-opacity-10 border border-2 border-primary' : 'bg-white'"
                @dragover.prevent="isDragging2 = true"
                @dragleave.prevent="isDragging2 = false"
                @drop.prevent="(e) => handleDrop(e, 2)"
              >
                <div class="card-body d-flex flex-column align-items-center justify-content-center p-5 text-center" style="min-height: 350px;">
                  <div v-if="!file2">
                    <div class="mb-4 p-4 rounded-circle bg-primary bg-opacity-10 text-primary d-inline-flex shadow-sm">
                      <i class="bi bi-cloud-arrow-up-fill display-4"></i>
                    </div>
                    <h4 class="fw-bold mb-2 text-dark">Файл 2</h4>
                    <p class="text-muted mb-4 small">Перетягніть файл сюди або натисніть кнопку</p>
                    <label class="btn btn-outline-primary rounded-pill px-4 py-2 fw-bold border-2">
                      <i class="bi bi-folder-plus me-2"></i> Обрати файл
                      <input type="file" hidden @change="(e) => handleFileSelect(e, 2)" accept=".cs,.py,.js,.ts,.java,.cpp,.h,.html,.css,.php,.rb,.go" />
                    </label>
                  </div>
                  <div v-else>
                    <div class="mb-3 text-success position-relative">
                      <i class="bi bi-file-earmark-check-fill display-1"></i>
                      <span class="position-absolute top-0 start-100 translate-middle p-2 bg-success border border-light rounded-circle"></span>
                    </div>
                    <h5 class="fw-bold text-break mb-1 text-dark">{{ file2.name }}</h5>
                    <span class="badge bg-light text-secondary border mb-4 px-3 py-2 rounded-pill">{{ (file2.size / 1024).toFixed(2) }} KB</span>
                    <div>
                      <button class="btn btn-light text-danger btn-sm rounded-pill px-4 py-2 fw-bold shadow-sm" @click="removeFile(2)">
                        <i class="bi bi-trash3-fill me-1"></i> Видалити
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="row mt-5 justify-content-center">
            <div class="col-lg-9">
              <div class="card shadow-lg border-0 rounded-4 overflow-hidden">
                <div class="row g-0">

                  <div class="col-md-7 bg-white p-5">
                    <h5 class="fw-bold mb-4 d-flex align-items-center">
                      <i class="bi bi-sliders2 text-primary me-2"></i> Налаштування
                    </h5>

                    <label for="levelRange" class="form-label text-muted small fw-bold text-uppercase mb-3">
                      Глибина перевірки
                    </label>

                    <div class="d-flex align-items-center mb-3">
                      <span class="display-6 fw-bold text-dark me-2">{{ level }}</span>
                      <span class="text-muted">/ 5</span>
                    </div>

                    <input type="range" class="form-range mb-4" min="1" max="5" step="1" id="levelRange" v-model="level">

                    <div class="d-flex justify-content-between text-muted small fw-medium">
                      <span>Швидкість</span>
                      <span>Баланс</span>
                      <span>Точність</span>
                    </div>
                  </div>

                  <div class="col-md-5 bg-primary bg-gradient text-white p-5 d-flex flex-column justify-content-center align-items-center text-center position-relative overflow-hidden">
                    <div class="position-relative z-1 w-100">
                      <h4 class="fw-bold mb-2">Готові до аналізу?</h4>
                      <p class="text-white-50 mb-4 small">Перевірте вибрані файли та налаштування.</p>

                      <button
                        class="btn btn-light btn-lg w-100 rounded-pill shadow fw-bold text-primary py-3"
                        @click="startComparison"
                        :disabled="!file1 || !file2"
                      >
                        <i class="bi bi-lightning-charge-fill me-2"></i> Запустити
                      </button>
                    </div>
                  </div>

                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <AnalyzerPage v-else key="analyzer" />

    </transition>
  </div>
</template>

<style scoped>
.main-wrapper {
  overflow-x: hidden;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.4s ease, transform 0.4s ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>