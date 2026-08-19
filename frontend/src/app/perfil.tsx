import { useEffect, useRef, useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { PerfilForm } from '@/components/organisms/PerfilForm';
import { Topbar } from '@/components/organisms/Topbar';
import { atualizarNome, buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Carrega o nome atual do usuário autenticado ao montar (issue #27). Separado
 * de `PerfilScreen` só para caber no limite de 20 linhas por função
 * (`code-style.md`). `editadoRef` evita que a resposta do fetch inicial
 * sobrescreva silenciosamente uma edição já iniciada pelo usuário antes
 * dela chegar (achado de qa-review, PR #40).
 */
function usePerfilCarregado(token: string | null): [string, (nome: string) => void, (nome: string) => void] {
  const [nome, setNome] = useState('');
  const editadoRef = useRef(false);

  useEffect(() => {
    if (!token) {
      return;
    }
    void buscarPerfil().then((resultado) => {
      if (resultado.sucesso && !editadoRef.current) {
        setNome(resultado.nome);
      }
    });
  }, [token]);

  function onChangeNome(novoNome: string) {
    editadoRef.current = true;
    setNome(novoNome);
  }

  return [nome, onChangeNome, setNome];
}

/**
 * Salva a edição do nome, controlando erro/sucesso/estado de envio.
 * Separado de `PerfilScreen` pelo mesmo motivo de `usePerfilCarregado`.
 */
function useSalvarNome(nome: string, atualizarNomeLocal: (nome: string) => void) {
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [sucesso, setSucesso] = useState(false);
  const [salvando, setSalvando] = useState(false);

  async function handleSalvar() {
    setSalvando(true);
    setErro(undefined);
    setSucesso(false);

    const resultado = await atualizarNome(nome);

    setSalvando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    atualizarNomeLocal(resultado.nome);
    setSucesso(true);
  }

  return { erro, sucesso, salvando, handleSalvar };
}

/**
 * Tela de perfil (issue #27): permite ao usuário autenticado corrigir o
 * próprio nome — canal explícito já que um segundo cadastro com o mesmo
 * contato não sobrescreve o nome existente (RN da issue #20).
 */
export default function PerfilScreen() {
  const { carregando, token } = useSessao();
  useRedirecionarSemSessao(carregando, token);
  const [nome, onChangeNome, definirNome] = usePerfilCarregado(token);
  const { erro, sucesso, salvando, handleSalvar } = useSalvarNome(nome, definirNome);

  if (carregando || !token) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Meu perfil" />
      <View
        className="w-full flex-1 self-center items-center justify-center gap-four px-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <PerfilForm
          nome={nome}
          erro={erro}
          sucesso={sucesso}
          salvando={salvando}
          onChangeNome={onChangeNome}
          onSalvar={handleSalvar}
        />
      </View>
    </SafeAreaView>
  );
}
