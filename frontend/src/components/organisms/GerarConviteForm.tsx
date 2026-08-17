import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';

export type GerarConviteFormProps = {
  contato: string;
  erro?: string;
  enviando: boolean;
  onChangeContato: (contato: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de geração de convite (issue #2) — só o contato do
 * Aluno, sem campo de nome (o nome é preenchido pelo próprio Aluno no
 * aceite, tela `convite/[token]`). Não conhece a Api — apenas emite os
 * callbacks recebidos por prop, mesmo racional de `CadastroProfessorForm`.
 */
export function GerarConviteForm({
  contato,
  erro,
  enviando,
  onChangeContato,
  onSubmit,
}: GerarConviteFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Contato do Aluno"
        value={contato}
        onChangeText={onChangeContato}
        placeholder="E-mail ou telefone"
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Gerando...' : 'Gerar convite'}
        onPress={onSubmit}
        disabled={enviando}
      />
    </View>
  );
}
