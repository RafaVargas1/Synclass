import { useState } from 'react';
import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { FormField } from '@/components/molecules/FormField';
import { contatoEhValido, mascararContato, MensagemContatoInvalido } from '@/lib/validacaoContato';

export type CadastroUsuarioFormProps = {
  nome: string;
  contato: string;
  erro?: string;
  enviando: boolean;
  /**
   * `true` quando `onBlurContato` (issue #27) já detectou que o contato
   * pertence a uma identidade existente — trava o campo Nome (que a Api
   * ignoraria de qualquer forma, RN da issue #20) e orienta a corrigir pelo
   * perfil depois de logado, em vez de deixar o usuário preencher um valor
   * descartado silenciosamente.
   */
  nomeReadonly: boolean;
  onChangeNome: (nome: string) => void;
  onChangeContato: (contato: string) => void;
  onBlurContato: () => void;
  onSubmit: () => void;
};

/**
 * Organismo: formulário de cadastro de usuário (nome + contato) — os campos
 * são idênticos entre Professor (issue #1) e Aluno (issue #61), então as
 * duas telas compartilham este único componente em vez de duplicá-lo (ver
 * Critérios técnicos da issue #61). Não conhece a Api — apenas emite os
 * callbacks recebidos por prop, para que a tela (que conhece a Api)
 * controle o fluxo de envio.
 *
 * `erro` é exibido como mensagem geral do formulário (não anexada a um
 * campo específico): a Api devolve só uma mensagem de texto, sem indicar a
 * qual campo ela se refere (pode ser sobre o nome, o contato, ou a conexão),
 * então anexá-la a um campo fixo induziria o usuário a erro.
 */
export function CadastroUsuarioForm({
  nome,
  contato,
  erro,
  enviando,
  nomeReadonly,
  onChangeNome,
  onChangeContato,
  onBlurContato,
  onSubmit,
}: CadastroUsuarioFormProps) {
  const [erroContato, setErroContato] = useState<string | undefined>(undefined);

  function handleSubmit() {
    if (!contatoEhValido(contato)) {
      setErroContato(MensagemContatoInvalido);
      return;
    }
    setErroContato(undefined);
    onSubmit();
  }

  return (
    <View className="w-full gap-four">
      <FormField
        label="Nome"
        value={nome}
        onChangeText={onChangeNome}
        placeholder="Seu nome completo"
        editable={!nomeReadonly}
      />
      {nomeReadonly ? (
        <Paragraph>Contato já cadastrado. Para corrigir o nome, edite pelo perfil depois de logado.</Paragraph>
      ) : null}
      <FormField
        label="Contato"
        value={contato}
        onChangeText={(texto) => onChangeContato(mascararContato(texto, contato))}
        onBlur={onBlurContato}
        placeholder="E-mail ou telefone"
        errorMessage={erroContato}
      />
      {erro ? <ErrorMessage>{erro}</ErrorMessage> : null}
      <Button
        label={enviando ? 'Enviando...' : 'Cadastrar'}
        onPress={handleSubmit}
        disabled={enviando}
      />
    </View>
  );
}
