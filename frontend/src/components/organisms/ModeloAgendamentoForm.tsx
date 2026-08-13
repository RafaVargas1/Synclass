import { useState } from 'react';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { ChipSelector, type ChipSelectorOption } from '@/components/molecules/ChipSelector';
import { ModeloAgendamento } from '@/lib/api/configuracao';

export type ModeloAgendamentoFormProps = {
  enviando: boolean;
  erro?: string;
  onSubmit: (modeloAgendamento: ModeloAgendamento) => void;
};

const OpcoesModeloAgendamento: readonly ChipSelectorOption<ModeloAgendamento>[] = [
  { valor: ModeloAgendamento.Vago, rotulo: 'Vago' },
  { valor: ModeloAgendamento.Fixo, rotulo: 'Fixo' },
  { valor: ModeloAgendamento.Hibrido, rotulo: 'Híbrido' },
];
const ModeloInicial = ModeloAgendamento.Vago;

/**
 * Organismo: formulário de escolha do modelo de agendamento do Professor
 * (issue #7). Consumido pelo gate de `professor/[professorId]/horarios.tsx`
 * quando a configuração ainda não existe (404) — ver
 * docs/specs/7-modelo-agendamento/implementation.md#edge-points.
 */
export function ModeloAgendamentoForm({ enviando, erro, onSubmit }: ModeloAgendamentoFormProps) {
  const [modeloAgendamento, setModeloAgendamento] = useState(ModeloInicial);

  return (
    <View className="w-full gap-four">
      <ChipSelector
        label="Modelo de agendamento"
        opcoes={OpcoesModeloAgendamento}
        valor={modeloAgendamento}
        onChange={setModeloAgendamento}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Salvando...' : 'Definir modelo'}
        onPress={() => onSubmit(modeloAgendamento)}
        disabled={enviando}
      />
    </View>
  );
}
