import { act, renderHook, waitFor } from '@testing-library/react-native';

import { lerPapeis, lerToken, salvarPapeis, salvarToken } from '@/lib/auth/sessao';

import { SessaoProvider, useSessao } from './contexto-sessao';

jest.mock('@/lib/auth/sessao', () => ({
  lerToken: jest.fn(),
  lerPapeis: jest.fn(),
  salvarToken: jest.fn(),
  salvarPapeis: jest.fn(),
}));

const lerTokenMock = lerToken as jest.Mock;
const lerPapeisMock = lerPapeis as jest.Mock;
const salvarTokenMock = salvarToken as jest.Mock;
const salvarPapeisMock = salvarPapeis as jest.Mock;

describe('useSessao', () => {
  beforeEach(() => {
    lerTokenMock.mockReset();
    lerPapeisMock.mockReset();
    salvarTokenMock.mockReset();
    salvarPapeisMock.mockReset();
    salvarTokenMock.mockResolvedValue(undefined);
    salvarPapeisMock.mockResolvedValue(undefined);
  });

  it('exposes the papeis and defaults papelAtivo to the first papel once the session loads', async () => {
    lerTokenMock.mockResolvedValue('token-jwt');
    lerPapeisMock.mockResolvedValue(['Professor', 'Aluno']);

    const { result } = await renderHook(() => useSessao(), { wrapper: SessaoProvider });

    await waitFor(() => expect(result.current.carregando).toBe(false));
    expect(result.current.papeis).toEqual(['Professor', 'Aluno']);
    expect(result.current.papelAtivo).toBe('Professor');
  });

  it('exposes an empty papeis list and undefined papelAtivo when there is no saved session', async () => {
    lerTokenMock.mockResolvedValue(null);
    lerPapeisMock.mockResolvedValue(null);

    const { result } = await renderHook(() => useSessao(), { wrapper: SessaoProvider });

    await waitFor(() => expect(result.current.carregando).toBe(false));
    expect(result.current.papeis).toEqual([]);
    expect(result.current.papelAtivo).toBeUndefined();
  });

  it('definirPapelAtivo switches the active papel', async () => {
    lerTokenMock.mockResolvedValue('token-jwt');
    lerPapeisMock.mockResolvedValue(['Professor', 'Aluno']);

    const { result } = await renderHook(() => useSessao(), { wrapper: SessaoProvider });
    await waitFor(() => expect(result.current.carregando).toBe(false));

    await act(async () => {
      result.current.definirPapelAtivo('Aluno');
    });

    expect(result.current.papelAtivo).toBe('Aluno');
  });

  it('definirSessao persists the new token/papeis and updates the in-memory state immediately', async () => {
    lerTokenMock.mockResolvedValue(null);
    lerPapeisMock.mockResolvedValue(null);

    const { result } = await renderHook(() => useSessao(), { wrapper: SessaoProvider });
    await waitFor(() => expect(result.current.carregando).toBe(false));

    await act(async () => {
      await result.current.definirSessao('token-novo', ['Professor', 'Aluno']);
    });

    expect(salvarTokenMock).toHaveBeenCalledWith('token-novo');
    expect(salvarPapeisMock).toHaveBeenCalledWith(['Professor', 'Aluno']);
    expect(result.current.token).toBe('token-novo');
    expect(result.current.papeis).toEqual(['Professor', 'Aluno']);
    expect(result.current.papelAtivo).toBe('Professor');
  });
});
