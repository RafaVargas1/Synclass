import { useState } from 'react';

import type { CadastroUsuarioInput, CadastroUsuarioResultado, VerificarContatoResultado } from '@/lib/api/cadastroUsuario';

export type ClienteCadastroUsuario = {
  cadastrar: (input: CadastroUsuarioInput) => Promise<CadastroUsuarioResultado>;
  verificarContato: (contato: string) => Promise<VerificarContatoResultado>;
};

export type OpcoesCadastroUsuario = {
  /**
   * Valor inicial do campo de contato (issue #65): preenchido quando a tela
   * já sabe o e-mail — vindo de `?email=` na rota (Google com cadastro
   * pendente). Serve só como valor inicial de estado; se o usuário digitar
   * algo depois, o que ele digitou prevalece.
   */
  contatoInicial?: string;
};

/**
 * Estado e handlers da tela de cadastro de usuário (nome + contato) —
 * Professor (issue #1) e Aluno (issue #61) só diferem no `cliente` de Api
 * injetado (ver `lib/api/cadastroUsuario.ts`) e no texto exibido pela tela,
 * então compartilham este hook em vez de duplicar o fluxo de estado.
 */
export function useCadastroUsuario(
  cliente: ClienteCadastroUsuario,
  opcoes?: OpcoesCadastroUsuario,
) {
  const [nome, setNome] = useState('');
  const [contato, setContato] = useState(opcoes?.contatoInicial ?? '');
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const [concluido, setConcluido] = useState(false);
  const [nomeReadonly, setNomeReadonly] = useState(false);

  async function handleBlurContato() {
    const resultado = await cliente.verificarContato(contato);
    setNomeReadonly(resultado.identidadeExistente);
    if (resultado.identidadeExistente && resultado.nome) {
      setNome(resultado.nome);
    }
  }

  function handleChangeContato(contatoNovo: string) {
    setContato(contatoNovo);
    // Contato mudou depois de já ter passado pela verificação — o resultado
    // anterior (readonly com o nome de outra identidade) não vale mais até
    // o próximo blur confirmar de novo.
    setNomeReadonly(false);
  }

  async function handleSubmit() {
    setEnviando(true);
    setErro(undefined);

    const resultado = await cliente.cadastrar({ nome, contato });

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConcluido(true);
  }

  return {
    nome,
    contato,
    erro,
    enviando,
    concluido,
    nomeReadonly,
    setContato,
    setNome,
    handleChangeContato,
    handleBlurContato,
    handleSubmit,
  };
}
