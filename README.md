# FAT

Во время проектирования файловой системы была спроектирована логическая разметка диска, которая представленна ниже.

| Главная запись | Файл MFT | Файл ASFS | Файл GROUPS | Файл USERS | Блоки данных | Копия файла | MFT Блоки данных | Копия главной записи |
| -------------- | -------- | --------- | ----------- | ---------- | ------------ | ----------- | ---------------- | -------------------- |
