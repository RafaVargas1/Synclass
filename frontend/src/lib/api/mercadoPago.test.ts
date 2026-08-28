import { conectarMercadoPago } from '@/lib/api/mercadoPago';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('conectarMercadoPago', () => {
  it('returns the authorization url when the Api responds with 200', async () => {
    mockFetchOnce(200, { url: 'https://auth.mercadopago.com/authorization?client_id=1' });

    const resultado = await conectarMercadoPago('professor-1');

    expect(resultado).toEqual({
      sucesso: true,
      url: 'https://auth.mercadopago.com/authorization?client_id=1',
    });
  });

  it('gets the fixed conectar route', async () => {
    mockFetchOnce(200, { url: 'https://auth.mercadopago.com/authorization' });

    await conectarMercadoPago('professor-1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/mercado-pago/conectar'),
      expect.anything(),
    );
  });

  it('returns sucesso false when the Api responds with an error status', async () => {
    mockFetchOnce(500, null);

    const resultado = await conectarMercadoPago('professor-1');

    expect(resultado.sucesso).toBe(false);
  });

  it('returns sucesso false when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await conectarMercadoPago('professor-1');

    expect(resultado.sucesso).toBe(false);
  });
});
