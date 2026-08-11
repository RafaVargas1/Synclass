import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
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
 *
 * `erro` é exibido como mensagem geral do formulário (não anexada a um
 * campo específico): a Api devolve só uma mensagem de texto, sem indicar a
 * qual campo ela se refere (pode ser sobre o nome, o contato, ou a conexão),
 * então anexá-la a um campo fixo induziria o usuário a erro.
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
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Enviando...' : 'Cadastrar'}
        onPress={onSubmit}
        disabled={enviando}
      />
    </View>
  );
}
