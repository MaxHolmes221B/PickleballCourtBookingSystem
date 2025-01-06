package com.maxholmes.androidapp.screen.customer.bookschedule.detail

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import com.maxholmes.androidapp.data.dto.request.CancelBookingRequest
import com.maxholmes.androidapp.data.dto.response.APIResponse
import com.maxholmes.androidapp.data.model.Booking
import com.maxholmes.androidapp.data.service.RetrofitClient
import com.maxholmes.androidapp.screen.customer.bookschedule.BookScheduleActivity
import com.maxholmes.androidapp.utils.enum.BookingStatusEnum
import com.maxholmes.androidapp.utils.ext.SharedPreferencesUtils
import com.maxholmes.androidapp.databinding.ActivityBookingCustomerDetailBinding
import com.maxholmes.androidapp.utils.ext.toCustomDateFormat
import com.maxholmes.androidapp.utils.ext.toCustomDateTimeFormat
import com.pickleballcourtbookingsystem.api.dtos.CustomBookingResponse
import retrofit2.Call
import retrofit2.Callback
import retrofit2.Response

class BookingCustomerDetailActivity : AppCompatActivity() {

    private lateinit var binding: ActivityBookingCustomerDetailBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityBookingCustomerDetailBinding.inflate(layoutInflater)
        setContentView(binding.root)

        val customBookingResponse: CustomBookingResponse? = intent.getParcelableExtra("bookingDetail", CustomBookingResponse::class.java)
        val token = SharedPreferencesUtils.getToken(this)
        val booking = customBookingResponse?.booking

        if (booking?.status == BookingStatusEnum.Pending) {
            binding.cancelButton.visibility = Button.VISIBLE
        } else {
            binding.cancelButton.visibility = Button.GONE
        }

        val lastUpdateTime = "Thời gian cập nhật cuối: ${customBookingResponse?.lastUpdatedTime!!.toCustomDateTimeFormat()}"
        var dateUse = "Ngày dùng sân: ${customBookingResponse?.courtTimeSlots?.get(0)?.date!!.toCustomDateFormat()}"
        if(customBookingResponse.booking!!.status == BookingStatusEnum.Canceled)
        {
            dateUse = "Ngày dùng sân: ${customBookingResponse?.booking.timeBooking.toCustomDateTimeFormat()}"
        }
        val price = "Tổng giá tiền: ${customBookingResponse?.booking?.amount}"
        val courtOwnerPhoneNumber = "Số điện thoại chủ sân: ${customBookingResponse?.courtOwnerPhoneNumber}"
        binding.tvCourtClusterName.text = customBookingResponse?.courtClusterName
        binding.tvAddress.text = customBookingResponse?.address.toString()
        binding.tvTimeBooking.text = lastUpdateTime
        binding.tvDateUse.text = dateUse
        binding.tvStatusValue.text = when (customBookingResponse!!.booking!!.status) {
            BookingStatusEnum.Pending -> "Đang chờ xử lý"
            BookingStatusEnum.CourtOwnerConfirmed -> "Đã được xác nhận"
            BookingStatusEnum.Canceled -> "Đã hủy"
            BookingStatusEnum.All -> "Tất cả trạng thái"
        }
        binding.tvPriceValue.text = price
        binding.tvPhone.text = courtOwnerPhoneNumber

        binding.recyclerViewTimeSlots.layoutManager = LinearLayoutManager(this)
        val timeSlotAdapter = CourtTimeSlotBookingDetailAdapter(customBookingResponse?.courtTimeSlots ?: listOf())
        binding.recyclerViewTimeSlots.adapter = timeSlotAdapter

        binding.cancelButton.setOnClickListener {
            if (token != null) {
                cancelBooking(token, booking)
            }
        }
    }

    private fun cancelBooking(token: String, booking: Booking?) {
        if (booking != null) {
            val tokenHeader = "Bearer $token"
            val cancelBookingRequest = CancelBookingRequest(
                bookingId = booking.id
            )
            RetrofitClient.ApiClient.apiService.cancelBooking(cancelBookingRequest, tokenHeader).enqueue(object : Callback<APIResponse> {
                override fun onResponse(call: Call<APIResponse>, response: Response<APIResponse>) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@BookingCustomerDetailActivity, "Hủy booking thành công!", Toast.LENGTH_SHORT).show()
                        val intent = Intent(this@BookingCustomerDetailActivity, BookScheduleActivity::class.java)
                        startActivity(intent)
                        finish()
                    } else {
                        Toast.makeText(this@BookingCustomerDetailActivity, "Lỗi khi hủy booking. Vui lòng thử lại!", Toast.LENGTH_SHORT).show()
                    }
                }

                override fun onFailure(call: Call<APIResponse>, t: Throwable) {
                    Toast.makeText(this@BookingCustomerDetailActivity, "Lỗi kết nối mạng. Vui lòng thử lại!", Toast.LENGTH_SHORT).show()
                }
            })
        } else {
            Toast.makeText(this, "Không tìm thấy booking để hủy!", Toast.LENGTH_SHORT).show()
        }
    }
}
