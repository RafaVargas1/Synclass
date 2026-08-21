import { ChipSelectorOption } from '@/components/molecules/ChipSelector';
import { TipoMarcacao } from '@/lib/api/horarios';

/**
 * Opções do seletor de política de marcação, compartilhadas entre
 * `HorarioForm` (issue #76) e `HorarioCard` (issue #71) para não duplicar o
 * array de chips (ver docs/spec/code-style.md#estilo-de-código — "sem
 * duplicação de código").
 */
export const OpcoesTipoMarcacao: readonly ChipSelectorOption<TipoMarcacao>[] = [
  { valor: TipoMarcacao.Livre, rotulo: 'Livre' },
  { valor: TipoMarcacao.Fixo, rotulo: 'Fixo' },
  { valor: TipoMarcacao.Hibrido, rotulo: 'Híbrido' },
];
