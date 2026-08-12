import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';

export type SolicitarCodigoFormProps = {
  contato: string;
  erro?: string;
  enviando: boolean;
  onChangeContato: (contato: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de solicitação de login por código (issue #18). Não
 * conhece a Api — mesmo padrão de CadastroProfessorForm, só emite os
 * callbacks recebidos por prop.
 */
export function SolicitarCodigoForm({
  contato,
  erro,
  enviando,
  onChangeContato,
  onSubmit,
}: SolicitarCodigoFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Contato"
        value={contato}
        onChangeText={onChangeContato}
        placeholder="E-mail ou telefone cadastrado"
        autoCapitalize="none"
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Enviando...' : 'Enviar código'}
        onPress={onSubmit}
        disabled={enviando}
      />
    </View>
  );
}
