# Task: Repensar o separador do Topbar, sem o motivo zigue-zague atual (#68)

Card: https://github.com/RafaVargas1/Synclass/issues/68

## Ordem de execução

- [x] Teste de componente: `Topbar` — não referencia mais `ZigzagDivider` (remove o teste/import legado se existir; o `border-b border-border` do container externo já é a única linha divisória)
- [x] Implementação: remove `<ZigzagDivider />` de `Topbar.tsx` e o import correspondente
- [x] Implementação: remove `frontend/src/components/atoms/ZigzagDivider.tsx` (sem outros usos no repo)
- [x] Docs: nenhuma atualização necessária em `design-system.md` — o divisor passa a ser só o padrão de borda já documentado em "Forma e elevação", não introduz técnica nova

### Inconsistências encontradas

_(Nenhuma — `Topbar.tsx` já renderiza `border-b border-border` no `View` externo, que envolve tanto o conteúdo quanto o `ZigzagDivider`. Remover o `ZigzagDivider` já satisfaz os dois critérios de aceite: a borda de 1px é nativa do RN/nativewind, funciona igual em web e nativo, e já usa o token `border` do design system — sem precisar de nenhuma solução nova.)_
