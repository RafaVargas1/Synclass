import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { FormField } from '@/components/molecules/FormField';

export type CadastroAlunoProvisorioFormProps = {
  nome: string;
  identificador: string;
  erro?: string;
  enviando: boolean;
  onChangeNome: (nome: string) => void;
  onChangeIdentificador: (identificador: string) => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de cadastro de Aluno provisório (nome +
 * identificador, issue #3) — sem campo de contato, diferente de
 * `CadastroProfessorForm` (issue #1): um Aluno provisório nunca exige
 * e-mail, telefone ou login. Não conhece a Api — apenas emite os callbacks
 * recebidos por prop.
 *
 * `erro` é exibido como mensagem geral do formulário, mesmo racional de
 * `CadastroProfessorForm`: a Api devolve só uma mensagem de texto, sem
 * indicar a qual campo ela se refere.
 */
export function CadastroAlunoProvisorioForm({
  nome,
  identificador,
  erro,
  enviando,
  onChangeNome,
  onChangeIdentificador,
  onSubmit,
}: CadastroAlunoProvisorioFormProps) {
  return (
    <View className="w-full gap-four">
      <FormField
        label="Nome"
        value={nome}
        onChangeText={onChangeNome}
        placeholder="Nome do Aluno"
      />
      <FormField
        label="Identificador"
        value={identificador}
        onChangeText={onChangeIdentificador}
        placeholder="Identificador (ex: número de matrícula)"
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
