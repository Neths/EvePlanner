import {baseApi} from "./baseApi";

const charactersApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getAll: builder.query({
      query: () => 'characters/'
    }),
    delete: builder.query({
      query: (id) => ({
        url: `characters/${id}`,
        method: 'DELETE'
      }),
    })
  }),
  overrideExisting: false
})

export const { useGetAllQuery } = charactersApi