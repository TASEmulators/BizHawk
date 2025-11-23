# 📥 Download Compiled APK / Baixar APK Compilado

## English

### Where to Download the Compiled APK?

**The compiled APK is automatically built and available for download!**

#### Option 1: Latest Development Build (Recommended)
1. Go to **[GitHub Actions - Build APK Workflow](../../actions/workflows/build-and-upload-apk.yml)**
2. Click on the **most recent successful workflow run** (green checkmark ✅)
3. Scroll down to **"Artifacts"** section
4. Download **BizHawkRafaelia-APK-[commit-hash].zip**
5. Extract the ZIP file to get the APK

#### Option 2: Stable Release Build
1. Go to **[Releases Page](../../releases)**
2. Download the APK file from the latest release
3. The APK will be named: `BizHawkRafaelia-unsigned-arm64-v8a.apk`

#### Option 3: Build Locally
If you prefer to build the APK yourself:
```bash
./generate-apk.sh
```
See [APK_GENERATION_README.md](APK_GENERATION_README.md) for detailed instructions.

### Installation

Once you have the APK file:

```bash
# Connect your Android device via USB with USB debugging enabled
adb install BizHawkRafaelia-unsigned-arm64-v8a.apk
```

Or simply transfer the APK to your device and install it from the file manager.

### Important Notes

⚠️ **This is an UNSIGNED APK** - meant for testing and development purposes.

✅ **Features included:**
- ARM64 NEON SIMD optimizations
- Rafaelia performance framework
- Zero-allocation memory pooling
- Thermal throttling mitigation
- Adaptive hardware quality management

📱 **Requirements:**
- Android 7.0 (API 24) or higher
- ARM64-v8a device (most modern Android phones)
- ~50-100MB storage space

---

## Português (Portuguese)

### Onde Baixar o APK Compilado?

**O APK compilado é construído automaticamente e está disponível para download!**

#### Opção 1: Build de Desenvolvimento Mais Recente (Recomendado)
1. Acesse **[GitHub Actions - Workflow de Build APK](../../actions/workflows/build-and-upload-apk.yml)**
2. Clique na **execução de workflow mais recente bem-sucedida** (marca verde ✅)
3. Role para baixo até a seção **"Artifacts"**
4. Baixe **BizHawkRafaelia-APK-[commit-hash].zip**
5. Extraia o arquivo ZIP para obter o APK

#### Opção 2: Build de Release Estável
1. Acesse a **[Página de Releases](../../releases)**
2. Baixe o arquivo APK do release mais recente
3. O APK terá o nome: `BizHawkRafaelia-unsigned-arm64-v8a.apk`

#### Opção 3: Compilar Localmente
Se você preferir compilar o APK você mesmo:
```bash
./generate-apk.sh
```
Veja [APK_GENERATION_README.md](APK_GENERATION_README.md) para instruções detalhadas.

### Instalação

Depois de ter o arquivo APK:

```bash
# Conecte seu dispositivo Android via USB com depuração USB ativada
adb install BizHawkRafaelia-unsigned-arm64-v8a.apk
```

Ou simplesmente transfira o APK para seu dispositivo e instale-o pelo gerenciador de arquivos.

### Notas Importantes

⚠️ **Este é um APK NÃO ASSINADO** - destinado para testes e desenvolvimento.

✅ **Recursos incluídos:**
- Otimizações ARM64 NEON SIMD
- Framework de performance Rafaelia
- Pool de memória com zero alocações
- Mitigação de throttling térmico
- Gerenciamento adaptativo de qualidade de hardware

📱 **Requisitos:**
- Android 7.0 (API 24) ou superior
- Dispositivo ARM64-v8a (maioria dos celulares Android modernos)
- ~50-100MB de espaço de armazenamento

### FAQ

**P: Por que o APK não está assinado?**
R: Para distribuição pública, você deve assinar o APK com sua própria keystore. O APK não assinado é para testes.

**P: Como assinar o APK?**
R: Veja as instruções em [APK_GENERATION_README.md](APK_GENERATION_README.md) seção "Sign APK (Production)".

**P: O APK não está aparecendo nos Releases?**
R: Ele será adicionado automaticamente quando um novo release for criado. Use GitHub Actions para builds de desenvolvimento.

**P: Posso instalar em qualquer dispositivo Android?**
R: Apenas dispositivos ARM64 (64-bit). A maioria dos dispositivos modernos são compatíveis.

---

## Quick Links / Links Rápidos

- 🔨 [GitHub Actions (Latest Builds)](../../actions/workflows/build-and-upload-apk.yml)
- 📦 [Releases (Stable Builds)](../../releases)
- 📖 [Build Instructions](APK_GENERATION_README.md)
- 🐛 [Report Issues](../../issues)
- 💬 [Discussions](../../discussions)

---

**No more asking "where is the compiled APK?" - It's right here! / Não precisa mais perguntar "cadê o APK compilado?" - Está aqui!** 🎉
