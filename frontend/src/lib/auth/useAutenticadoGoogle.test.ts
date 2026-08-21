import { act, renderHook } from '@testing-library/react-native';

import { useSessao } from '@/lib/auth/contexto-sessao';

import { useAutenticadoGoogle } from './useAutenticadoGoogle';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ replace: mockRouterReplace }),
}));

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

const useSessaoMock = useSessao as jest.Mock;
const definirSessaoMock = jest.fn();

describe('useAutenticadoGoogle', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    definirSessaoMock.mockReset();
    useSessaoMock.mockReset();
    useSessaoMock.mockReturnValue({ definirSessao: definirSessaoMock });
  });

  it('persists the session and navigates to /painel on success', async () => {
    definirSessaoMock.mockResolvedValue(undefined);
    const { result } = await renderHook(() => useAutenticadoGoogle());

    await act(async () => {
      await result.current.handleAutenticadoGoogle({ token: 't', nome: 'Ana', papeis: ['Professor'] });
    });

    expect(definirSessaoMock).toHaveBeenCalledWith('t', ['Professor']);
    expect(mockRouterReplace).toHaveBeenCalledWith('/painel');
    expect(result.current.erroGoogle).toBeUndefined();
  });

  it('sets erroGoogle and does not navigate when persisting the session fails', async () => {
    definirSessaoMock.mockRejectedValue(new Error('falhou'));
    const { result } = await renderHook(() => useAutenticadoGoogle());

    await act(async () => {
      await result.current.handleAutenticadoGoogle({ token: 't', nome: 'Ana', papeis: ['Professor'] });
    });

    expect(mockRouterReplace).not.toHaveBeenCalled();
    expect(result.current.erroGoogle).toBe(
      'Não foi possível concluir o login neste dispositivo. Tente novamente.',
    );
  });
});
