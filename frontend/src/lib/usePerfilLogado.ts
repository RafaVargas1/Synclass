import { useEffect, useState } from 'react';

import { buscarPerfil } from '@/lib/api/usuarios';

export type EstadoPerfilLogado = {
  usuarioId: string | undefined;
  nome: string | undefined;
  contato: string | undefined;
  erro: boolean;
  tentarNovamente: () => void;
};

/**
 * Resolve o perfil do usuário logado via `GET /usuarios/me` (issue #44,
 * #69 e #77): qualquer papel com `token` busca, já que `nome` alimenta a
 * saudação do Painel (Professor e Aluno) e `usuarioId` só é relevante pros
 * links de Professor (ver `secoesProfessor`). `contato` (issue #144) é
 * exposto para a tela de entrar em nova turma enviar o contato do Aluno
 * logado junto do código de convite, sem pedir de novo. `MenuNavegacao`
 * reusa este hook só para o `usuarioId` — ver decisão em `task.md` (não
 * usar prop extra). Para de buscar assim que resolve uma vez (guarda por
 * `usuarioId` já preenchido): sem isso, alternar entre papéis via
 * `AlternadorDePapel` refaria a chamada a cada troca, mesmo o Professor não
 * podendo ter um `usuarioId` diferente na mesma sessão (achado de
 * dev-review, PR #55). Em caso de falha, expõe `erro` e `tentarNovamente`
 * em vez de deixar as ações do Professor sumirem sem explicação nem forma
 * de recuperar (mesmo achado).
 */
export function usePerfilLogado(token: string | null): EstadoPerfilLogado {
  const [usuarioId, setUsuarioId] = useState<string | undefined>(undefined);
  const [nome, setNome] = useState<string | undefined>(undefined);
  const [contato, setContato] = useState<string | undefined>(undefined);
  const [erro, setErro] = useState(false);
  const [tentativa, setTentativa] = useState(0);

  useEffect(() => {
    if (!token || usuarioId) {
      return;
    }
    let cancelado = false;
    void buscarPerfil().then((resultado) => {
      if (cancelado) return;
      if (!resultado.sucesso) {
        setErro(true);
        return;
      }
      setErro(false);
      setUsuarioId(resultado.usuarioId);
      setNome(resultado.nome);
      setContato(resultado.contato);
    });
    return () => {
      cancelado = true;
    };
  }, [token, tentativa, usuarioId]);

  return { usuarioId, nome, contato, erro, tentarNovamente: () => setTentativa((atual) => atual + 1) };
}
