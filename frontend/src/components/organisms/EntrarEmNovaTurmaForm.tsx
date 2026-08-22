import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';

export type EntrarEmNovaTurmaFormProps = {
  codigo: string;
  erro?: string;
  enviando: boolean;
  onChangeCodigo: (codigo: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de entrada em nova turma via código de convite
 * (issue #144) — só pede o código; nome/contato do Aluno já autenticado
 * são enviados por trás das cenas (ver `entrar-em-turma.tsx`), sem exibir
 * campo pra eles.
 */
export function EntrarEmNovaTurmaForm({
  codigo,
  erro,
  enviando,
  onChangeCodigo,
  onSubmit,
}: EntrarEmNovaTurmaFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Código da turma"
        value={codigo}
        onChangeText={onChangeCodigo}
        placeholder="00000"
        keyboardType="number-pad"
        maxLength={5}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Entrando...' : 'Entrar na turma'}
        onPress={onSubmit}
        disabled={enviando}
      />
    </View>
  );
}
