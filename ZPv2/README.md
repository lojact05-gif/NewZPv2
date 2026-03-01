# ZPv2 (Windows)

Nova versão do serviço de impressão local do POS.

## Princípios de UI
- UI minimalista conforme Manual da Marca Zaldo.
- Apenas 3 elementos: status, token (16 chars) e botão copiar.
- Configuração de impressora/gaveta/corte fica no POS (`pos_configuracoes.php`).

## Componentes
- `ZPv2.Service` (Windows Service + API local)
- `ZPv2.Ui` (app desktop minimalista)
- `ZPv2.Common` (config/token/log/ESC-POS/RAW spool)

## API local
Base URL padrão: `http://127.0.0.1:16262`

- `GET /health`
- `GET /token`
- `POST /token/regenerate`
- `POST /print` (autenticado por token)

Headers:
- `X-ZPV2-TOKEN: <token 16 chars>`

## Token
- Sempre exatamente 16 caracteres (`A-Z0-9`).
- Persistência em ProgramData com fallback para LocalAppData.

## Instalação cliente final
1. Baixar `ZPv2Setup.exe`.
2. Executar o wizard (Next/Install/Finish).
3. Confirmar serviço `ZPv2Service` no `services.msc`.
4. Abrir app `ZPv2` e copiar token.
5. Configurar token/endpoint/impressora no POS.

## Build local
```powershell
cd tools\ZPv2\scripts
./build.ps1 -Configuration Release -Runtime win-x64 -SelfContained:$true
./build_installer.ps1 -InnoPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Saída:
- `tools/ZPv2/out/publish/_stage/*`
- `tools/ZPv2/out/installer/ZPv2Setup.exe`
