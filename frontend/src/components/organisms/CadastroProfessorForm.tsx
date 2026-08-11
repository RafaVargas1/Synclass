import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { FormField } from '@/components/molecules/FormField';

export type CadastroProfessorFormProps = {
  nome: string;
  contato: string;
  erro?: string;
  enviando: boolean;
  onChangeNome: (nome: string) => void;
  onChangeContato: (contato: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de cadastro de Professor (nome + contato). Não
 * conhece a Api — apenas emite os callbacks recebidos por prop, para que a
 * tela (que conhece a Api) controle o fluxo de envio.
 */
export function CadastroProfessorForm({
  nome,
  contato,
  erro,
  enviando,
  onChangeNome,
  onChangeContato,
  onSubmit,
}: CadastroProfessorFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Nome"
        value={nome}
        onChangeText={onChangeNome}
        placeholder="Seu nome completo"
      />
      <FormField
        label="Contato"
        value={contato}
        onChangeText={onChangeContato}
        placeholder="E-mail ou telefone"
        errorMessage={erro}
      />
      <Button
        label={enviando ? 'Enviando...' : 'Cadastrar'}
        onPress={onSubmit}
        disabled={enviando}
      />
    </View>
  );
}
