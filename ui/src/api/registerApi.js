import {baseApi} from "./baseApi";


const registerApi = baseApi.injectEndpoints({
    endpoints: (build) => ({
        register: build.query({
            query: () => 'register/'
        })
    }),
    overrideExisting: false
})

export const { useRegisterQuery } = registerApi