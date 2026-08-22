import { act, renderHook, waitFor } from '@testing-library/react-native';

import { buscarPerfil } from '@/lib/api/usuarios';

import { usePerfilLogado } from './usePerfilLogado';

jest.mock('@/lib/api/usuarios', () => ({
  buscarPerfil: jest.fn(),
}));

const buscarPerfilMock = buscarPerfil as jest.Mock;

describe('usePerfilLogado', () => {
  beforeEach(() => {
    buscarPerfilMock.mockReset();
  });

  it('não busca o perfil sem token', () => {
    renderHook(() => usePerfilLogado(null));

    expect(buscarPerfilMock).not.toHaveBeenCalled();
  });

  it('resolve usuarioId, nome e contato quando o perfil carrega com sucesso', async () => {
    buscarPerfilMock.mockResolvedValue({
      sucesso: true,
      usuarioId: 'prof-1',
      nome: 'Ana',
      contato: 'ana@exemplo.com',
    });

    const { result } = await renderHook(() => usePerfilLogado('token-jwt'));

    await waitFor(() => expect(result.current.usuarioId).toBe('prof-1'));
    expect(result.current.nome).toBe('Ana');
    expect(result.current.contato).toBe('ana@exemplo.com');
    expect(result.current.erro).toBe(false);
  });

  it('não refaz a busca depois de já ter resolvido uma vez', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });

    const { result, rerender } = await renderHook(
      ({ token }: { token: string }) => usePerfilLogado(token),
      { initialProps: { token: 'token-jwt' } },
    );

    await waitFor(() => expect(result.current.usuarioId).toBe('prof-1'));

    await act(async () => {
      rerender({ token: 'token-jwt' });
    });
    await act(async () => {
      rerender({ token: 'token-jwt' });
    });

    expect(buscarPerfilMock).toHaveBeenCalledTimes(1);
  });

  it('marca erro quando o perfil falha, e refaz a busca ao chamar tentarNovamente', async () => {
    buscarPerfilMock
      .mockResolvedValueOnce({ sucesso: false, mensagem: 'erro' })
      .mockResolvedValueOnce({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });

    const { result } = await renderHook(() => usePerfilLogado('token-jwt'));

    await waitFor(() => expect(result.current.erro).toBe(true));
    expect(result.current.usuarioId).toBeUndefined();

    await act(async () => {
      result.current.tentarNovamente();
    });

    await waitFor(() => expect(result.current.usuarioId).toBe('prof-1'));
    expect(result.current.erro).toBe(false);
    expect(buscarPerfilMock).toHaveBeenCalledTimes(2);
  });
});
