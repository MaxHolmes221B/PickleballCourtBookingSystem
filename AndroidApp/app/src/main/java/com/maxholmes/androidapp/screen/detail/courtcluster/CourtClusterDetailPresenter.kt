package com.maxholmes.androidapp.screen.detail.courtcluster

import com.maxholmes.androidapp.data.repository.CourtClusterRepository
import com.maxholmes.androidapp.screen.home.HomeContract

class CourtClusterDetailPresenter (private val mCourtRepository: CourtClusterRepository) :
    HomeContract.Presenter {
        private var mView: HomeContract.View? = null

        override fun onStart() {
            TODO("Not yet implemented")
        }

        override fun onStop() {
            TODO("Not yet implemented")
        }

        override fun setView(view: HomeContract.View?) {
            this.mView = view
        }
}