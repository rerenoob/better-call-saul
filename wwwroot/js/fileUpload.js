// Client-side file validation and preview functionality
class FileUploadHelper {
    constructor() {
        this.maxFileSize = 10 * 1024 * 1024; // 10MB
        this.supportedTypes = ['.pdf', '.docx'];
        this.supportedMimeTypes = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    }

    validateFile(file) {
        // Check file size
        if (file.size > this.maxFileSize) {
            return {
                isValid: false,
                error: `File size exceeds 10MB limit. Current size: ${this.formatFileSize(file.size)}`
            };
        }

        // Check file type by extension
        const extension = this.getFileExtension(file.name);
        if (!this.supportedTypes.includes(extension.toLowerCase())) {
            return {
                isValid: false,
                error: `File type not supported. Please upload PDF or DOCX files only.`
            };
        }

        return { isValid: true, error: null };
    }

    getFileExtension(filename) {
        return filename.slice((filename.lastIndexOf(".") - 1 >>> 0) + 2);
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    createPreview(file, container) {
        if (file.type.startsWith('image/')) {
            this.createImagePreview(file, container);
        } else {
            this.createDocumentPreview(file, container);
        }
    }

    createImagePreview(file, container) {
        const reader = new FileReader();
        reader.onload = (e) => {
            const img = document.createElement('img');
            img.src = e.target.result;
            img.className = 'img-thumbnail';
            img.style.maxWidth = '200px';
            img.style.maxHeight = '200px';
            container.innerHTML = '';
            container.appendChild(img);
        };
        reader.readAsDataURL(file);
    }

    createDocumentPreview(file, container) {
        const extension = this.getFileExtension(file.name);
        const iconClass = this.getFileIconClass(extension);
        
        container.innerHTML = `
            <div class="document-preview">
                <i class="${iconClass} preview-icon"></i>
                <div class="preview-info">
                    <div class="file-name">${file.name}</div>
                    <div class="file-size">${this.formatFileSize(file.size)}</div>
                </div>
            </div>
        `;
    }

    getFileIconClass(extension) {
        switch (extension.toLowerCase()) {
            case 'pdf': return 'bi bi-file-earmark-pdf';
            case 'docx': return 'bi bi-file-earmark-word';
            case 'doc': return 'bi bi-file-earmark-word';
            default: return 'bi bi-file-earmark';
        }
    }

    // Drag and drop helpers
    setupDragAndDrop(dropZone, fileInput) {
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, this.preventDefaults, false);
        });

        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, () => {
                dropZone.classList.add('drag-over');
            }, false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, () => {
                dropZone.classList.remove('drag-over');
            }, false);
        });

        dropZone.addEventListener('drop', (e) => {
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                fileInput.files = files;
                const event = new Event('change', { bubbles: true });
                fileInput.dispatchEvent(event);
            }
        }, false);
    }

    preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Progress tracking
    createProgressBar(container, fileName) {
        container.innerHTML = `
            <div class="upload-progress">
                <div class="progress-info">
                    <span class="file-name">${fileName}</span>
                    <span class="progress-percent">0%</span>
                </div>
                <div class="progress">
                    <div class="progress-bar" role="progressbar" style="width: 0%" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100"></div>
                </div>
            </div>
        `;

        return {
            update: (percentage) => {
                const progressBar = container.querySelector('.progress-bar');
                const progressPercent = container.querySelector('.progress-percent');
                if (progressBar && progressPercent) {
                    progressBar.style.width = percentage + '%';
                    progressBar.setAttribute('aria-valuenow', percentage);
                    progressPercent.textContent = percentage + '%';
                }
            },
            complete: () => {
                const progressBar = container.querySelector('.progress-bar');
                if (progressBar) {
                    progressBar.classList.add('bg-success');
                }
            },
            error: () => {
                const progressBar = container.querySelector('.progress-bar');
                if (progressBar) {
                    progressBar.classList.add('bg-danger');
                }
            }
        };
    }
}

// Initialize global instance
window.fileUploadHelper = new FileUploadHelper();