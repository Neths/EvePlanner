import { configureStore } from '@reduxjs/toolkit'
import modalSlice from "../slices/modalSlice";
import { baseApi } from "../api/baseApi";

export const store = configureStore({
  reducer: {
    [baseApi.reducerPath]: baseApi.reducer,
    modal: modalSlice
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware({
    serializableCheck:{
      ignoreActions: [],
      ignoredPaths: ['modal.actionButtonOnClick', 'payload.actionButtonOnClick']
    }
  })
      .concat(baseApi.middleware),
  devTools: true
})