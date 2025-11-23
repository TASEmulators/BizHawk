# BizHawk Rafaelia - Android APK

## 🚀 QUER O APK PRONTO? BAIXE AQUI!

**Não quer compilar? Baixe o APK já compilado direto do GitHub Actions!**

👉 **[CLIQUE AQUI PARA BAIXAR O APK](../../DOWNLOAD_APK.md)** 👈

Ou veja: [CADÊ O APK? AQUI!](../../CADE_O_APK.md) (versão em português)

---

Este projeto permite gerar um APK Android não assinado e compilado do BizHawk Rafaelia otimizado para ARM64.

## Pré-requisitos

1. **.NET SDK 8.0 ou superior**
   ```bash
   dotnet --version
   ```

2. **.NET Android workload**
   ```bash
   dotnet workload install android
   ```

3. **Android SDK** (opcional, mas recomendado)
   - Baixe em: https://developer.android.com/studio

## Gerar APK Não Assinado

Execute o script de geração:

```bash
./generate-apk.sh
```

O script irá:
1. ✓ Verificar pré-requisitos
2. ✓ Limpar builds anteriores
3. ✓ Restaurar dependências
4. ✓ Compilar módulos Rafaelia
5. ✓ Gerar APK não assinado
6. ✓ Copiar para `output/android/`

## Saída

O APK será gerado em:
```
output/android/BizHawkRafaelia-unsigned-arm64-v8a.apk
```

## Instalar em Dispositivo

### Opção 1: Instalar diretamente (para testes)
```bash
adb install output/android/BizHawkRafaelia-unsigned-arm64-v8a.apk
```

### Opção 2: Assinar o APK (para distribuição)

1. Gerar keystore (apenas uma vez):
```bash
keytool -genkey -v -keystore my-release-key.keystore \
  -alias my-key-alias -keyalg RSA -keysize 2048 -validity 10000
```

2. Assinar o APK:
```bash
apksigner sign --ks my-release-key.keystore \
  --out BizHawkRafaelia-signed.apk \
  output/android/BizHawkRafaelia-unsigned-arm64-v8a.apk
```

## Otimizações Incluídas

✓ **Framework de performance Rafaelia**
- Pooling de memória (zero-allocation)
- SIMD ARM64 NEON
- Processamento paralelo

✓ **Otimizações para Mobile**
- Gerenciamento adaptativo de hardware
- Algoritmos eficientes em energia
- Mitigação de throttling térmico

✓ **Suporte ARM64**
- Compilado para ARM64-v8a
- Otimizado para Android 7.0+ (API 24)
- Suporte para dispositivos modernos

## Especificações Técnicas

- **Target Framework**: .NET 8.0 Android
- **Minimum SDK**: Android 7.0 (API 24)
- **Target SDK**: Android 13 (API 33)
- **Architecture**: ARM64-v8a
- **Package Format**: APK (não assinado)

## Troubleshooting

### Erro: ".NET Android workload not found"
```bash
dotnet workload install android
```

### Erro: "Android SDK not found"
1. Instale o Android Studio
2. Configure a variável de ambiente:
   ```bash
   export ANDROID_HOME=/path/to/android/sdk
   ```

### Erro: "APK build failed"
Verifique se todas as dependências estão instaladas:
```bash
dotnet workload list
dotnet restore src/BizHawk.Android/BizHawk.Android.csproj
```

## Informações Adicionais

Para mais informações sobre o projeto BizHawk Rafaelia e suas otimizações:
- Repository: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
- Documentation: Veja `/rafaelia/README.md`
- Performance Report: Veja `/rafaelia/OPTIMIZATION_REPORT_RAFAELIA.md`

## Licença

Este projeto é um fork do BizHawk (TASEmulators) e segue as mesmas licenças.
- Fork Parent: https://github.com/TASEmulators/BizHawk
- Maintainer: Rafael Melo Reis
