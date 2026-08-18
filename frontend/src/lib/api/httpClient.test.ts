import { fetchComTimeout } from '@/lib/api/httpClient';
import { lerToken } from '@/lib/auth/sessao';

jest.mock('@/lib/auth/sessao', () => ({
  lerToken: jest.fn(),
}));

function mockFetchOnce() {
  globalThis.fetch = jest.fn().mockResolvedValue({ ok: true, status: 200 }) as jest.Mock;
}

describe('fetchComTimeout', () => {
  it('adiciona o header Authorization quando há token de sessão salvo', async () => {
    (lerToken as jest.Mock).mockResolvedValue('token-jwt');
    mockFetchOnce();

    await fetchComTimeout('/professores/id/convites', { method: 'POST' });

    const [, init] = (globalThis.fetch as jest.Mock).mock.calls[0];
    const headers = init.headers as Headers;
    expect(headers.get('Authorization')).toBe('Bearer token-jwt');
  });

  it('não adiciona o header Authorization quando não há sessão salva', async () => {
    (lerToken as jest.Mock).mockResolvedValue(null);
    mockFetchOnce();

    await fetchComTimeout('/professores/cadastro', { method: 'POST' });

    const [, init] = (globalThis.fetch as jest.Mock).mock.calls[0];
    const headers = init.headers as Headers;
    expect(headers.has('Authorization')).toBe(false);
  });

  it('preserva os demais headers passados pelo chamador', async () => {
    (lerToken as jest.Mock).mockResolvedValue('token-jwt');
    mockFetchOnce();

    await fetchComTimeout('/professores/id/convites', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });

    const [, init] = (globalThis.fetch as jest.Mock).mock.calls[0];
    const headers = init.headers as Headers;
    expect(headers.get('Content-Type')).toBe('application/json');
    expect(headers.get('Authorization')).toBe('Bearer token-jwt');
  });
});
