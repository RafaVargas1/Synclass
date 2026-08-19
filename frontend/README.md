# Synclass (app)

App Expo + TypeScript do Synclass — gera web e mobile a partir do mesmo
código. Ver decisões de arquitetura em
[`../docs/spec/architecture.md`](../docs/spec/architecture.md).

## Setup local

```bash
npm install
npm run web       # versão web (http://localhost:8081)
npm start          # QR code para abrir no Expo Go (mobile)
```

Por padrão aponta para a Api de dev em `http://localhost:5005` (ver
[`backend/README.md`](../backend/README.md#setup-local)) — sobrescreva com a
variável `EXPO_PUBLIC_API_URL` se a Api estiver em outro host/porta (ex:
`:8080` no container do `docker compose`, usado por `scripts/qa-web-static.sh`).

## Scripts

| Comando | O que faz |
|---|---|
| `npm run web` / `npm start` / `npm run android` / `npm run ios` | Sobe o app |
| `npm run lint` | ESLint (`eslint-config-expo`) |
| `npm run typecheck` | `tsc --noEmit` |
| `npm run format` | Prettier (com plugin de ordenação de classes Tailwind) |
| `npm test` | Jest (`jest-expo` + React Native Testing Library) |

## Atomic Design

Componentes organizados em `src/components/{atoms,molecules,organisms,templates}`:

- **atoms**: menor unidade reutilizável (`Button`, `Heading`, `Paragraph`). Sem regra de negócio.
- **molecules**: composição de átomos com propósito único (`IntroSection`).
- **organisms**: seções completas de UI, podem ter estado/interação (`HomeHero`).
- **templates**: layout de página sem dados reais (`HomeTemplate`).
- **rotas** (`src/app/*.tsx`, Expo Router): montam o template com dados reais.

Estilização via [NativeWind](https://www.nativewind.dev/) (Tailwind para RN).
Os valores de design (cor, espaçamento, raio) têm fonte única em
`src/theme/palette.js`, consumido tanto por `tailwind.config.js` quanto pelo
wrapper tipado `src/theme/tokens.ts` — nenhum componente deve usar um valor
de estilo fora desses tokens.

## Testes

Todo componente novo (especialmente átomos e moléculas) ganha um teste em
`*.test.tsx` ao lado do arquivo, usando `@testing-library/react-native`.
Lembre-se: nessa versão da lib, `render()` é assíncrono — use `await render(...)`.
