import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { PerfilForm } from '@/components/organisms/PerfilForm';
import { atualizarNome, buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';

/**
 * Carrega o nome atual do usuário autenticado ao montar (issue #27). Separado
 * de `PerfilScreen` só para caber no limite de 20 linhas por função
 * (`code-style.md`).
 */
function usePerfilCarregado(token: string | null): [string, (nome: string) => void] {
  const [nome, setNome] = useState('');

  useEffect(() => {
    if (!token) {
      return;
    }
    void buscarPerfil().then((resultado) => {
      if (resultado.sucesso) {
        setNome(resultado.nome);
      }
    });
  }, [token]);

  return [nome, setNome];
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
  const [nome, setNome] = usePerfilCarregado(token);
  const { erro, sucesso, salvando, handleSalvar } = useSalvarNome(nome, setNome);

  if (carregando || !token) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 items-center justify-center gap-four px-four">
        <Heading level={1}>Meu perfil</Heading>
        <PerfilForm
          nome={nome}
          erro={erro}
          sucesso={sucesso}
          salvando={salvando}
          onChangeNome={setNome}
          onSalvar={handleSalvar}
        />
      </View>
    </SafeAreaView>
  );
}
