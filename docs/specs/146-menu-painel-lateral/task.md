# Task: menu mobile vira painel lateral de altura cheia (#146)

Card: https://github.com/RafaVargas1/Synclass/issues/146

Leia `implementation.md` ANTES do primeiro item — código pronto pra colar.

## Ordem de execução

- [ ] Teste (`MenuNavegacao.test.tsx`): adicione os dois testes novos
      descritos em `implementation.md` (style `position: fixed`/
      `top`/`left`/`bottom`; ausência de `rounded-medium`/`min-w-[220px]`
      na classe do modo mobile). Ver falhar.
- [ ] Implementação mínima: aplique a mudança de `implementation.md` no
      bloco do `dropdown-menu-navegacao`.
- [ ] Rode a suíte completa de `MenuNavegacao.test.tsx` — confirme que o
      teste de #129 (6 itens dentro do fundo opaco) continua passando.
- [ ] Verificação visual (Playwright, se disponível no ambiente — senão
      registre em "Inconsistências encontradas" que não foi possível
      verificar e siga pelos testes automatizados): abra o app real
      (`npx expo start --web`), injete sessão fake, viewport 390×844,
      abra o menu, confirme visualmente que o painel ocupa a altura
      inteira da tela.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
